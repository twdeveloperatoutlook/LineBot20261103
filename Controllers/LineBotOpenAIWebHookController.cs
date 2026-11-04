using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace isRock.Template
{
    public class LineBotOpenAIWebHookController : isRock.LineBot.LineWebHookControllerBase
    {
        [Route("api/LineBotOpenAIWebHook")]
        [HttpPost]
        public IActionResult POST()
        {
            const string AdminUserId = "___________________"; //👉repleace it with your Admin User Id

            try
            {
                //設定ChannelAccessToken
                this.ChannelAccessToken = "____________________________"; //👉repleace it with your Channel Access Token
                //配合Line Verify
                if (
                    ReceivedMessage?.events?.Count() <= 0
                    || ReceivedMessage?.events?.FirstOrDefault()?.replyToken
                        == "00000000000000000000000000000000"
                )
                    return Ok();
                //取得Line Event
                var LineEvent = this.ReceivedMessage.events.FirstOrDefault();
                var responseMsg = "";
                //準備回覆訊息
                if (LineEvent.type.ToLower() == "message" && LineEvent.message.type == "text")
                {
                    if (LineEvent.message.text.Contains("/reset"))
                    {
                        ChatHistoryManager.DeleteIsolatedStorageFile();
                        responseMsg = "我已經把之前的對談都給忘了!";
                    }
                    else
                    {
                        var chatHistory = ChatHistoryManager.GetMessagesFromIsolatedStorage(LineEvent.source.userId);
                        responseMsg = ChatGPT.getResponseFromGPT(LineEvent.message.text, chatHistory);
                        //儲存聊天紀錄
                        ChatHistoryManager.SaveMessageToIsolatedStorage(
                            System.DateTime.Now, LineEvent.source.userId, LineEvent.message.text, responseMsg);
                    }
                }
                else if (LineEvent.type.ToLower() == "message")
                    responseMsg = $"收到 event : {LineEvent.type} type: {LineEvent.message.type} ";
                else
                    responseMsg = $"收到 event : {LineEvent.type} ";
                //回覆訊息
                this.ReplyMessage(LineEvent.replyToken, responseMsg);
                //response OK
                return Ok();
            }
            catch (Exception ex)
            {
                //回覆訊息
                this.PushMessage(AdminUserId, "發生錯誤:\n" + ex.Message);
                //response OK
                return Ok();
            }
        }
    }

    public class LLM
    {
        const string OpenAIKey = "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"; //👉repleace it with your OpenAI API Key

        const string GitHubModelKey = "👉github_pat____________"; //👉repleace it with your GitHub Models API Key

        const string AzureOpenAIEndpoint = "https://👉_______.openai.azure.com"; //👉replace it with your Azure OpenAI Endpoint
        const string AzureOpenAIModelName = "👉AzureOpenAIModelDeployName"; //👉repleace it with your Azure OpenAI Model Deploy Name
        const string AzureOpenAIToken = "👉040d_____52a0d"; //👉repleace it with your Azure OpenAI API Key
        const string AzureOpenAIVersion = "2023-03-15-preview"; //👉replace  it with your Azure OpenAI API Version

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum role
        {
            assistant,
            user,
            system,
        }

        //Call Azure OpenAI API
        public static string CallAzureOpenAIChatAPI(
            string endpoint,
            string DeploymentName,
            string apiKey,
            string apiVersion,
            object requestData
        )
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl =
                $"{endpoint}/openai/deployments/{DeploymentName}/chat/completions?api-version={apiVersion}";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("api-key", apiKey); //👉Azure OpenAI key
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            ); //ACCEPT header
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new System.Exception($"Azure OpenAI API 回應錯誤：{responseContent}");
            // Json to Object
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            return obj.choices[0].message.content.Value;
        }

        //Call OpenAI API
        public static string CallOpenAIChatAPI(string apiKey, object requestData)
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl = $"https://api.openai.com/v1/chat/completions";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}"); //👉OpenAI key
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            ); //ACCEPT header
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new System.Exception($"OpenAI API 回應錯誤：{responseContent}");
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            return obj.choices[0].message.content.Value;
        }
        //Call OpenAI API
        public static string CallGitHubOpenAIChatAPI(string apiKey, object requestData)
        {
            var client = new HttpClient();

            // 設定 API 網址
            var apiUrl = $"https://models.github.ai/inference/chat/completions";

            // 設定 HTTP request headers
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}"); //👉GitHub Models key
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            ); //ACCEPT header
            // 將 requestData 物件序列化成 JSON 字串
            string jsonRequestData = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
            // 建立 HTTP request 內容
            var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
            // 傳送 HTTP POST request
            var response = client.PostAsync(apiUrl, content).Result;
            // 取得 HTTP response 內容
            var responseContent = response.Content.ReadAsStringAsync().Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new System.Exception($"GitHub Models API 回應錯誤：{responseContent}");
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
            return obj.choices[0].message.content.Value;
        }

        /// <summary>
        /// 取得 GPT 回應
        /// </summary>
        /// <param name="Message"> User message </param>
        /// <returns></returns>
        public static string getResponse(string Message)
        {
            //建立要傳給 OpenAI API 的訊息內容
            var MessageBody = new
            {
                model = "gpt-4o", //👉repleace it with your Azure OpenAI Model Deploy Name or OpenAI Model Name
                messages = new[]
                {
                    //👉You can set the system role to guide the behavior of the assistant.
                    new
                    {
                        role = ChatGPT.role.system,
                        content = @"
                        你是餐飲店的客服機器人，會回答客戶詢問的餐點問題，並且推薦讓店裡獲利最高的產品。你也能處裡客戶的抱怨和申訴，安撫客戶情緒。
                        目前店裡面所販賣的產品有底下這些:
                        大亨堡，45元
                        麥香雞，36元
                        蛋餅，27元
                        可麗餅，50元
                        飯糰，45元

                        而飲料有底下這些:
                        可樂，55元
                        紅茶，35元
                        奶茶，45元
                        店裡沒有套餐，只能把餐點和飲料搭配銷售，這樣搭配沒有折扣。此外，跟客人推薦餐點時，必須是上述的清單中的餐點。
                        目前唯一的優惠是周日早上買一送一，只限單點大亨堡才有這個優惠。
                        ----------------------
                        ",
                    },
                     new { role = ChatGPT.role.user, content = "請告訴我最便宜的餐點價格。" },
                      new { role = ChatGPT.role.assistant, content = " 你可以點選蛋餅，只需要27元" },
                    new { role = ChatGPT.role.user, content = Message }, //👉將使用者的訊息傳給 OpenAI API
                },
            };

            //呼叫 OpenAI API 並取得回應
            return CallOpenAIChatAPI(OpenAIKey, MessageBody); //👉repleace it with your OpenAI API Key

            //呼叫 GitHub Models API 並取得回應
            return CallGitHubOpenAIChatAPI(GitHubModelKey, MessageBody); //👉repleace it with your GitHub Models API Key

            //呼叫 Azure OpenAI API 並取得回應
            return CallAzureOpenAIChatAPI(
                AzureOpenAIEndpoint,
                AzureOpenAIModelName,
                AzureOpenAIToken,
                AzureOpenAIVersion,
                //ref: https://learn.microsoft.com/en-us/azure/cognitive-services/openai/reference#chat-completions
                MessageBody
            );
        }
    }
}
