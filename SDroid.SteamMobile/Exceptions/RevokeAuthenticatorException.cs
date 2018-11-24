﻿using System;
using SDroid.SteamMobile.Models.TwoFactorServiceAPI;

namespace SDroid.SteamMobile.Exceptions
{
    /// <summary>
    ///     Represent an error that happened during the process of revoking a registered authenticator
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class RevokeAuthenticatorException : Exception
    {
        internal RevokeAuthenticatorException(RemoveAuthenticatorResponse response) : base(
            "Failed to revoke the authenticator.")
        {
            AttemptsRemaining = response?.RevocationAttemptsRemaining;
        }

        /// <summary>
        ///     Gets the number of remaining possible attempts to revoke this authenticator
        /// </summary>
        /// <value>
        ///     Number of remaining attempts
        /// </value>
        public uint? AttemptsRemaining { get; }
    }
}