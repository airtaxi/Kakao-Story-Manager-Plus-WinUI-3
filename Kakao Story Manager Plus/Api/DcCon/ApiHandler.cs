using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KSMP.Api.DcCon
{
    public static class ApiHandler
    {
        public static async Task<byte[]> GetDcDonImage(string path, int retryCount = 0)
        {
            var url = "https://dcimg5.dcinside.com/dccon.php?no=" + path;
            var client = new RestClient(url);
            var data = await client.DownloadDataAsync(new RestRequest());
            if (data == null && retryCount < 5)
                return await GetDcDonImage(path, ++retryCount);
            return data;
        }

        public static async Task<DataType.Package> GetDcDonPackageDetailAsync(int id)
        {
            var client = new RestClient("https://dccon.dcinside.com/index/package_detail");
            var request = new RestRequest()
            {
                Method = Method.Post
            };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            request.AddHeader("X-Requested-With", "XMLHttpRequest");
            request.AddBody($"package_idx={id}");

            var response = await client.ExecuteAsync(request);
            var content = response.Content;
            if (content == "error") return null;
            else return JsonConvert.DeserializeObject<DataType.Package>(content);
        }
    }
}
