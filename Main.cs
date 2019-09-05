using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;

namespace Instagram_Creator
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private async void btnCreate_Click(object sender, EventArgs e)
        {
            try
            {
                successLabel.Text = "";
                errorLabel.Text = "";
                /// <summary>Setup the HttpClient, HttpHandler and CookieContainer</summary>
                CookieContainer instaCookies = new CookieContainer();
                using (HttpClientHandler Handler = new HttpClientHandler() { CookieContainer = instaCookies, UseCookies = true, AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip })
                using (HttpClient Client = new HttpClient(Handler))
                {
                    /// <summary>Set the referer header for our requests as Instragram checks to see if it exists</summary>
                    Client.DefaultRequestHeaders.Add("referer", "https://www.instagram.com/");

                    /// <summary>Set up the post data for when we send the account creation dry run to instagram</summary>
                    Dictionary<string, string> instagramPostData = new Dictionary<string, string>
            {
                { "email", txtMail.Text },
                { "password", txtPass.Text },
                { "username", txtUser.Text },
                { "first_name", txtName.Text }
            };

                    /// <summary>Make a GET request to Instagrams login page to set the site cookies inside our cookie container</summary>
                    HttpRequestMessage getInstaMsg = new HttpRequestMessage() { Method = HttpMethod.Get, RequestUri = new Uri("https://www.instagram.com/accounts/login/") };
                    HttpResponseMessage getInstaResponse = await Client.SendAsync(getInstaMsg);
                    string instaResponseString = await getInstaResponse.Content.ReadAsStringAsync();

                    /// <summary>Retrieve the csrftoken cookie from the cookie container and set it as a header as Instragram checks this too</summary>
                    IEnumerable<Cookie> responseCookies = Handler.CookieContainer.GetCookies(new Uri("https://www.instagram.com")).Cast<Cookie>();
                    Client.DefaultRequestHeaders.Add("x-csrftoken", responseCookies.FirstOrDefault(x => x.Name == "csrftoken").Value);

                    /// <summary>Make a POST request to Instagrams dry run account creation endpoint</summary>
                    HttpRequestMessage postInstaCheck = new HttpRequestMessage() { Method = HttpMethod.Post, RequestUri = new Uri("https://www.instagram.com/accounts/web_create_ajax/attempt/"), Content = new FormUrlEncodedContent(instagramPostData) };
                    getInstaResponse = await Client.SendAsync(postInstaCheck);

                    /// <summary>Deserialze the JSON response from Instragram in to a Object</summary>
                    instagramRootObject instagramResponse = JsonConvert.DeserializeObject<instagramRootObject>(await getInstaResponse.Content.ReadAsStringAsync());

                    /// <summary>Return a valid response if the dry run passed else return a invalid response</summary>
                    if (instagramResponse.dryrun_passed)
                    {
                        /// <summary>Now we set the 'seamless login' parameter for the actual account creation request</summary>
                        instagramPostData.Add("seamless_login_enabled", "0");

                        /// <summary>Make a POST request to Instagrams account creation endpoint</summary>
                        postInstaCheck = new HttpRequestMessage() { Method = HttpMethod.Post, RequestUri = new Uri("https://www.instagram.com/accounts/web_create_ajax/"), Content = new FormUrlEncodedContent(instagramPostData) };
                        getInstaResponse = await Client.SendAsync(postInstaCheck);

                        /// <summary>Deserialze the JSON response from Instragram in to a Object</summary>
                        instagramResponse = JsonConvert.DeserializeObject<instagramRootObject>(await getInstaResponse.Content.ReadAsStringAsync());

                        if (instagramResponse.account_created)
                        {
                            /// <summary>Read success response from Instragram and set 'user_id' field to 'userIdText' textbox text field</summary>
                            successLabel.Text = $"Success: Account '{instagramPostData["username"]}' created";
                            txtId.Text = instagramResponse.user_id;
                        }
                        else
                        {
                            /// <summary>Read errors from Instagrams response and set 'errorLabel' labels text to the error</summary>
                            if (instagramResponse.errors != null)
                            {
                                if (instagramResponse.errors.ip != null)
                                    errorLabel.Text = $"Error: Instragram is detecting current IP as proxy and denying registration";
                                else if (instagramResponse.errors.email != null)
                                    errorLabel.Text = $"Error: {instagramResponse.errors.email[0].code.Replace("_", " ")}";
                                else if (instagramResponse.errors.password != null)
                                    errorLabel.Text = $"Error: {instagramResponse.errors.password[0].code.Replace("_", " ")}";
                                else if (instagramResponse.errors.username != null)
                                    errorLabel.Text = $"Error: {instagramResponse.errors.username[0].code.Replace("_", " ")}";
                                else
                                    errorLabel.Text = "Error: Unknown error occured";
                            }
                            else
                                errorLabel.Text = "Error: Unknown error occured";
                        }
                    }
                    else
                    {
                        /// <summary>Read errors from Instagrams response and set 'errorLabel' labels text to the error</summary>
                        if (instagramResponse.errors != null)
                        {
                            if (instagramResponse.errors.ip != null)
                                errorLabel.Text = $"Error: Instragram is detecting current IP as proxy and denying registration";
                            else if (instagramResponse.errors.email != null)
                                errorLabel.Text = $"Error: {instagramResponse.errors.email[0].code.Replace("_", " ")}";
                            else if (instagramResponse.errors.password != null)
                                errorLabel.Text = $"Error: {instagramResponse.errors.password[0].code.Replace("_", " ")}";
                            else if (instagramResponse.errors.username != null)
                                errorLabel.Text = $"Error: {instagramResponse.errors.username[0].code.Replace("_", " ")}";
                            else
                                errorLabel.Text = "Error: Unknown error occured";
                        }
                        else
                            errorLabel.Text = "Error: Unknown error occured";
                    }
                }
            }
            catch (Exception ex)
            {
                /// <summary>Gets any program errors and set 'errorLabel' labels text to the error</summary>
                errorLabel.Text = $"Program Error: {ex.Message}";
            }
        }
    }

    public class errorMessage
    {
        public string message { get; set; }
        public string code { get; set; }
    }

    public class Errors
    {
        public List<string> ip { get; set; }
        public List<errorMessage> email { get; set; }
        public List<errorMessage> username { get; set; }
        public List<errorMessage> password { get; set; }
    }

    public class instagramRootObject
    {
        public bool account_created { get; set; }
        public string user_id { get; set; }
        public Errors errors { get; set; }
        public bool dryrun_passed { get; set; }
        public List<object> username_suggestions { get; set; }
        public string status { get; set; }
        public string error_type { get; set; }
    }
}
