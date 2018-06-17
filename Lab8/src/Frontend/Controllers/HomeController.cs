using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Frontend.Models;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;


namespace Frontend.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }
        
        private async Task<string> Get(String url) {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));                        

            var response = await httpClient.GetAsync(url);            
            string value = "";
            if (response.IsSuccessStatusCode)
            {
                value = await response.Content.ReadAsStringAsync();
            }
            else
            {
                value = response.StatusCode.ToString();
            }            

            return value;
        }  

        [HttpPost]
        public IActionResult Upload(string data)
        {
            string result = "";
            string url = "http://127.0.0.1:5000/api/values";
            if (data != null)
            {
                result = Post(url, data).Result;
            }
            string sendUrl = "http://127.0.0.1:5001/Home/TextDetails?=" + result;
            
            return new RedirectResult(sendUrl);
        }

        private async Task<string> Post(string url, string data)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            data = ("=" + data);            
            var content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await httpClient.PostAsync(url, content);
            var id = await response.Content.ReadAsStringAsync();

            return id;
        }  

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult TextDetails(string id)
        {        
            string url = "http://127.0.0.1:5000/api/values/";
            string value = Get(url + id).Result;
            ViewData["Msg"] = value;
            return View();
        }
    }
}
