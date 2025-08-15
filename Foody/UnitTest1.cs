using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;




namespace Foody
{
    [TestFixture]

    public class FoodyTests
    {
        private RestClient client;
        private static string createdFoodId;
        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("Gogo44", "gogo123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateFood_ShouldReturnCreated()
        {
            var food = new
            {
                name = "New Food",
                description = "Delicious new food item",
                url = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var jason = JsonSerializer.Deserialize<JsonElement>(response.Content);

            createdFoodId = jason.GetProperty("foodId").GetString() ?? string.Empty;   
            
            Assert.That(createdFoodId, Is.Not.Null.Or.Empty, "Food ID should not be null or empty.");
        }

        [Test, Order(2)]
        public void EditFoodTitle_ShouldReturnOk()
        {
            var changes = new[]
            {
                new { path = "/name", op = "replace", value = "Updated Food Name" },
            };

            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);

            request.AddJsonBody(changes);

            var response = client.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully edited"), "Expected success message.");
        }

        [Test, Order(3)]
        public void GetAllFood_FoodReturnList()
        {
            var request = new RestRequest("/api/Food/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK."); 
            
            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);

            Assert.That(foods, Is.Not.Empty, "Expected a non-empty list of foods.");
        }

        [Test, Order(4)]
        public void DeleteFood_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(response.Content.Contains("Deleted successfully!"), "Expected success message in response content.");
        }

        [Test, Order(5)]
        public void CreateFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var food = new
            {
                Name = "",
                Description = "This food item has no name",
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Test, Order(6)]
        public void EditeNonExistingFood_ShouldReturnNotFound()
        {
            string fakeId = "123";
            var changes = new[]
            {
                new { path = "/name", op = "replace", value = "New Title" }
            };

            var request = new RestRequest($"/api/Food/Edit/{fakeId}", Method.Patch);
            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues..."));
        }

        [Test, Order(7)]
        public void DeliteNonExistingFood_ShouldReturnBadRequest()
        {
            string fakeId = "123";
            var request = new RestRequest($"/api/Food/Delete/{fakeId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"), "Expected error message for non-existing food.");
        }

        [OneTimeTearDown] 
        public void Cleanup()
        {
            client?.Dispose();
        }

    }
}