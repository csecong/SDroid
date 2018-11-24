﻿using Newtonsoft.Json;
using SDroid.SteamTrade.Helpers;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAssetV2
    {
        [JsonProperty("amount")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long Amount { get; set; }

        [JsonProperty("appid")]
        public long AppId { get; set; }

        [JsonProperty("assetid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long AssetId { get; set; }

        [JsonProperty("classid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long ClassId { get; set; }

        [JsonProperty("contextid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long ContextId { get; set; }

        [JsonProperty("instanceid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long InstanceId { get; set; }

        public UserInventoryAsset ToSteamInventoryAsset()
        {
            return new UserInventoryAsset(AppId, ContextId, AssetId, ClassId, InstanceId, Amount);
        }
    }
}