using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace KSMP.Api.DcCon;

public static class DataType
{
    public class Package
    {
        public class Detail
        {
            [JsonProperty("idx")]
            public string Index { get; set; }

            [JsonProperty("package_idx")]
            public string PackageIndex { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("sort")]
            public string Sort { get; set; }

            [JsonProperty("ext")]
            public string Extension { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }
        }

        public class Info
        {
            [JsonProperty("package_idx")]
            public string PackageIndex { get; set; }

            [JsonProperty("seller_no")]
            public string SellerNumber { get; set; }

            [JsonProperty("seller_id")]
            public string SellerId { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("price")]
            public string Price { get; set; }

            [JsonProperty("period")]
            public string Period { get; set; }

            [JsonProperty("icon_cnt")]
            public string IconCnt { get; set; }

            [JsonProperty("state")]
            public string State { get; set; }

            [JsonProperty("open")]
            public string Open { get; set; }

            [JsonProperty("sale_count")]
            public string SaleCount { get; set; }

            [JsonProperty("reg_date")]
            public string RegDate { get; set; }

            [JsonProperty("seller_name")]
            public string SellerName { get; set; }

            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("seller_type")]
            public string SellerType { get; set; }

            [JsonProperty("mandoo")]
            public string Mandoo { get; set; }

            [JsonProperty("main_img_path")]
            public string MainImgPath { get; set; }

            [JsonProperty("list_img_path")]
            public string ListImgPath { get; set; }

            [JsonProperty("reg_date_short")]
            public string RegDateShort { get; set; }

            [JsonProperty("residual")]
            public bool Residual { get; set; }

            [JsonProperty("register")]
            public bool Register { get; set; }
        }

        [JsonProperty("info")]
        public Info PackageInfo { get; set; }

        [JsonProperty("detail")]
        public List<Detail> PackageDetail { get; set; }
    }
}