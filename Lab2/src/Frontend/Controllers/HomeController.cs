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

        [HttpPost]
        public IActionResult Upload(string data)
        {
            string id = "";
            //TODO: send data in POST request to backend and read returned id value from response
            string url = "http://127.0.0.1:5000/api/values";
            if(data != null)
            {
                id = Post(url, data).Result;
            }
            
            return Ok(id);
        }

        private async Task<string> Post(string url, string data)
        {
            var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new[] { 
                new KeyValuePair<string, string>("", data) 
            }); 
            var response = await httpClient.PostAsync(url, content);
            var id = await response.Content.ReadAsStringAsync();

            return id;
        }  

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
