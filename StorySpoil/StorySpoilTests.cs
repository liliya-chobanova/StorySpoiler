using System;
using System.Net;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;

namespace StorySpoiler
{
    [TestFixture]
    public class StorySpoilerApiTests
    {
        private RestClient client;
        private static string lastCreatedStoryId;

        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net/api/";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJhZjNjMDhmYy04MzM4LTRiYzItYTY0OC0xZDZhY2ViNTZhNzgiLCJpYXQiOiIwOC8xNi8yMDI1IDA2OjI2OjE5IiwiVXNlcklkIjoiODY0NWRkOTctZGI1Ni00ZDQwLThlMDEtMDhkZGRiMWExM2YzIiwiRW1haWwiOiJsaWx0ZXN0QGV4YW1wbGUuY29tIiwiVXNlck5hbWUiOiJMaWxUZXN0IiwiZXhwIjoxNzU1MzQ3MTc5LCJpc3MiOiJTdG9yeVNwb2lsX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiU3RvcnlTcG9pbF9XZWJBUElfU29mdFVuaSJ9.yJamxrNGEZMA6eoFQFgO7V2N7eslGjDwJotjb7wYm1Y";

        private const string Username = "LilTest";
        private const string Password = "liltest";

        [OneTimeSetUp]
        public void Setup()
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Try static token first
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(StaticToken)
            };
            this.client = new RestClient(options);

            // Test token validity
            var testRequest = new RestRequest("Story/All", Method.Get);
            var testResponse = client.Execute(testRequest);

            if (testResponse.StatusCode == HttpStatusCode.Unauthorized ||
                testResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                string newToken = GetJwtToken(Username, Password);
                this.client = new RestClient(new RestClientOptions(BaseUrl)
                {
                    Authenticator = new JwtAuthenticator(newToken)
                });
                Console.WriteLine("Obtained a fresh JWT token.");
            }
            else
            {
                Console.WriteLine("Using static JWT token.");
            }
        }

        private string GetJwtToken(string username, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("User/Authentication", Method.Post);
            request.AddJsonBody(new { userName = username, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using var doc = JsonDocument.Parse(response.Content);
                var token = doc.RootElement.GetProperty("accessToken").GetString();

                if (!string.IsNullOrWhiteSpace(token))
                    return token;
            }

            throw new InvalidOperationException($"Failed to authenticate. Status: {response.StatusCode}, Content: {response.Content}");
        }

        [Test, Order(1)]
        public void CreateStory_WithRequiredFields_ShouldReturnSuccess()
        {
            var storyRequest = new StoryDTO
            {
                Title = "My Test Story",
                Description = "Meowsies",
                Url = "https://www.artdesign.ph/wp-content/uploads/2024/05/typ130-No-Problems-Just-Meow-Meow-Poster-02.png"
            };

            var request = new RestRequest("Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);

            var response = this.client.Execute(request);
            Console.WriteLine("Create Response: " + response.Content);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content, options);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
            Assert.That(createResponse.StoryId, Is.Not.Null.And.Not.Empty);

            lastCreatedStoryId = createResponse.StoryId;
        }

        [Test, Order(2)]
        public void EditStory_ShouldReturnSuccess()
        {
            var storyRequest = new StoryDTO
            {
                Title = "Edited Story Title",
                Description = "Edited story description",
                Url = "https://www.artdesign.ph/wp-content/uploads/2024/05/typ130-No-Problems-Just-Meow-Meow-Poster-02.png"
            };

            var request = new RestRequest($"Story/Edit/{lastCreatedStoryId}", Method.Put);
            request.AddJsonBody(storyRequest);

            var response = this.client.Execute(request);
            Console.WriteLine("Edit Response: " + response.Content);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content, options);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStories_ShouldReturnList()
        {
            var request = new RestRequest("Story/All", Method.Get);
            var response = this.client.Execute(request);

            Console.WriteLine("GetAll Response: " + response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("title"));
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnSuccess()
        {
            var request = new RestRequest($"Story/Delete/{lastCreatedStoryId}", Method.Delete);
            var response = this.client.Execute(request);

            Console.WriteLine("Delete Response: " + response.Content);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content, options);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(deleteResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var storyRequest = new StoryDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);

            var response = this.client.Execute(request);
            Console.WriteLine("BadRequest Response: " + response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            var storyRequest = new StoryDTO
            {
                Title = "NonExistent",
                Description = "Trying to edit missing story"
            };

            var request = new RestRequest("Story/Edit/00000000-0000-0000-0000-000000000000", Method.Put);
            request.AddJsonBody(storyRequest);

            var response = this.client.Execute(request);
            Console.WriteLine("EditNonExisting Response: " + response.Content);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content, options);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(editResponse.Msg, Is.EqualTo("No spoilers..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            var request = new RestRequest("Story/Delete/00000000-0000-0000-0000-000000000000", Method.Delete);
            var response = this.client.Execute(request);

            Console.WriteLine("DeleteNonExisting Response: " + response.Content);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content, options);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(deleteResponse.Msg, Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
