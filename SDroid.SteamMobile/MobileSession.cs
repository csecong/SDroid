﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamMobile.Exceptions;
using SDroid.SteamMobile.Models.MobileAuthenticationAPI;
using SDroid.SteamMobile.Models.MobileLoginJson;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Models;

namespace SDroid.SteamMobile
{
    /// <summary>
    ///     Represents pair of OAuth and web session
    /// </summary>
    /// <seealso cref="System.IEquatable{SessionData}" />
    public class MobileSession : WebSession, IEquatable<MobileSession>
    {
        public const string ClientName = "android";

        public const string ClientVersion = "0 (2.1.3)";

        /// <summary>
        ///     Initializes a new instance of the <see cref="MobileSession" /> class.
        /// </summary>
        /// <param name="oAuthToken">The OAuth token.</param>
        /// <param name="steamId">The steam user identifier number.</param>
        /// <param name="steamLogin">The steam user login.</param>
        /// <param name="steamLoginSecure">The steam user login secure.</param>
        /// <param name="sessionId">The session identifier string.</param>
        /// <param name="rememberLoginToken">The session remember login token</param>
        /// <param name="steamMachineAuthenticationToken">The session steam guard machine authentication tokens</param>
        [JsonConstructor]
        // ReSharper disable once TooManyDependencies
        public MobileSession(
            string oAuthToken,
            ulong? steamId,
            string steamLogin,
            string steamLoginSecure,
            string sessionId,
            string rememberLoginToken,
            Dictionary<ulong, string> steamMachineAuthenticationToken) :
            base(steamId, steamLogin, steamLoginSecure, sessionId,
                rememberLoginToken, steamMachineAuthenticationToken)
        {
            WebCookies.Add(new Cookie("mobileClientVersion", ClientVersion, "/",
                CommunityCookieDomain));
            WebCookies.Add(new Cookie("mobileClient", ClientName, "/",
                CommunityCookieDomain));

            OAuthToken = oAuthToken;
            SteamCommunityId = steamId;
        }

        public MobileSession()
        {
            WebCookies.Add(new Cookie("mobileClientVersion", ClientVersion, "/",
                CommunityCookieDomain));
            WebCookies.Add(new Cookie("mobileClient", ClientName, "/",
                CommunityCookieDomain));
        }

        internal MobileSession(MobileLoginOAuthModel oAuth, string sessionId) :
            this(
                oAuth.OAuthToken,
                oAuth.SteamId,
                oAuth.SteamId + "%7C%7C" + oAuth.Token,
                oAuth.SteamId + "%7C%7C" + oAuth.TokenSecure,
                sessionId,
                null,
                new Dictionary<ulong, string>
                {
                    {oAuth.SteamId, oAuth.WebCookie}
                })
        {
        }

        /// <summary>
        ///     Gets the OAuth token
        /// </summary>
        public string OAuthToken { get; }

        /// <summary>
        ///     Gets the steam user identifier number.
        /// </summary>
        public new ulong? SteamCommunityId
        {
            get
            {
                var stringValue = WebCookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["steamid"]
                    ?.Value;

                if (!string.IsNullOrWhiteSpace(stringValue) && ulong.TryParse(stringValue, out var steamId))
                {
                    return steamId;
                }

                return base.SteamCommunityId;
            }
            protected set
            {
                WebCookies.Add(new Cookie("steamid", value?.ToString() ?? "", "/",
                    CommunityCookieDomain));
                base.SteamCommunityId = value;
            }
        }

        /// <inheritdoc />
        public bool Equals(MobileSession other)
        {
            return other != null &&
                   base.Equals(other) &&
                   OAuthToken == other.OAuthToken &&
                   SteamCommunityId == other.SteamCommunityId;
        }

        public static bool operator ==(MobileSession data1, MobileSession data2)
        {
            return Equals(data1, data2) || data1?.Equals(data2) == true;
        }

        public static bool operator !=(MobileSession data1, MobileSession data2)
        {
            return !(data1 == data2);
        }

        /// <inheritdoc />
        public override WebSession Clone()
        {
            return new MobileSession(
                OAuthToken,
                SteamCommunityId,
                SteamLogin,
                SteamLoginSecure,
                SessionId,
                RememberLoginToken,
                SteamMachineAuthenticationToken
            );
        }

        /// <inheritdoc />
        public override bool Equals(WebSession other)
        {
            if (other is MobileSession mobileSession)
            {
                return Equals(mobileSession);
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as MobileSession);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -823311899;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SessionId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamLogin);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamLoginSecure);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(OAuthToken);
            hashCode = hashCode * -1521134295 + SteamCommunityId.GetHashCode();

            return hashCode;
        }

        /// <summary>
        ///     Determines whether this instance holds enough information to be considered as a valid representation of a
        ///     logged in session.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance holds enough information; otherwise, <c>false</c>.
        /// </returns>
        public override bool HasEnoughInfo()
        {
            return base.HasEnoughInfo() &&
                   !string.IsNullOrWhiteSpace(OAuthToken) &&
                   SteamCommunityId > 0;
        }

        /// <summary>
        ///     Refreshes the Steam session. Necessary to perform confirmations if your session has expired or changed.
        /// </summary>
        /// <returns>true if the operation completed successfully; otherwise false</returns>
        public async Task<bool> RefreshSession(SteamMobileWebAccess mobileWebAccess)
        {
            try
            {
                var serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(
                    () => new SteamWebAPI(mobileWebAccess)
                        .RequestObject<SteamWebAPIResponse<GetWGTokenResponse>>(
                            "IMobileAuthService",
                            SteamWebAccessRequestMethod.Post,
                            "GetWGToken",
                            "v0001",
                            new
                            {
                                access_token = OAuthToken
                            }
                        )
                ).ConfigureAwait(false);

                if (string.IsNullOrEmpty(serverResponse?.Response?.Token) &&
                    string.IsNullOrEmpty(serverResponse?.Response?.TokenSecure))
                {
                    return false;
                }

                SteamLogin = !string.IsNullOrWhiteSpace(serverResponse.Response?.Token)
                    ? SteamCommunityId + "%7C%7C" + serverResponse.Response.Token
                    : null;
                SteamLoginSecure = !string.IsNullOrWhiteSpace(serverResponse.Response?.TokenSecure)
                    ? SteamCommunityId + "%7C%7C" + serverResponse.Response.TokenSecure
                    : null;

                return true;
            }
            catch (WebException e)
            {
                var response = e.Response as HttpWebResponse;

                //Redirecting -- likely to a steammobile:// URI
                if (response?.StatusCode == HttpStatusCode.Found)
                {
                    var location = response.Headers.Get("Location");

                    if (!string.IsNullOrEmpty(location))
                    {
                        //Our OAuth token has expired. This is given both when we must refresh our session, or the entire OAuth Token cannot be refreshed anymore.
                        //Thus, we should only throw this exception when we're attempting to refresh our session.
                        if (location == "steammobile://lostauth")
                        {
                            throw new TokenExpiredException(e);
                        }
                    }
                }
            }

            return false;
        }

        public void UpdateSession(WebSession webSession)
        {
            SessionId = webSession.SessionId;
            SteamLogin = webSession.SteamLogin;
            SteamLoginSecure = webSession.SteamLoginSecure;
            RememberLoginToken = webSession.RememberLoginToken;
        }
    }
}