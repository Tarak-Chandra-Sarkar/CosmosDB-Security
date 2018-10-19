using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//https://docs.microsoft.com/en-us/azure/cosmos-db/secure-access-to-data

namespace ResourceSvc
{
    class Program
    {
        private static DocumentClient Client;
        private static DateTime BeginningOfTime = new DateTime(2017, 1, 1);
        static void Main(string[] args)
        {
       
            Permission p;
            String masterKey = ConfigurationManager.AppSettings["MASTER-KEY"];
            Uri endPoint = new Uri(ConfigurationManager.AppSettings["END-POINT"]);
            string databaseId = ConfigurationManager.AppSettings["DATABASE"];
            string collection = ConfigurationManager.AppSettings["COLLECTION"];
            Client = new DocumentClient(endPoint, masterKey);
            string userId = "Smith";


            p = new Permission
            {
                PermissionMode = PermissionMode.Read,
                ResourceLink = UriFactory.CreateDocumentCollectionUri(databaseId, collection).ToString(),
                ResourcePartitionKey = null,// partitionKey, // If not set, everyone can access every document
                Id = userId + databaseId + collection + "read" //needs to be unique for a given user
            };
        
            //Create a user for a database 
            User user = CreateUserIfNotExistAsync(databaseId, userId);
            PermissionToken pToken = GetResourceToken(user, p, databaseId);
            Console.WriteLine(pToken.Token);
            Console.Read();

        }

        private static User CreateUserIfNotExistAsync(string databaseId, string userId)
        {
            try
            {
                var t = Client.ReadUserAsync(UriFactory.CreateUserUri(databaseId, userId));
                t.Wait();
                return t.Result;
            }
            catch (Exception e)
            {
                var user = Client.CreateUserAsync(UriFactory.CreateDatabaseUri(databaseId), new User { Id = userId });
                user.Wait();
                return user.Result;
            }

        }



        private static PermissionToken GetResourceToken (User user, Permission p, string databaseId)
        {
            int? expires;
            int expireInSeconds = 600;
            List<Permission> permissions = GetAllPermissions(user, databaseId);
            foreach (Permission per in permissions)
            {
                if(per.Id==p.Id)
                {
                    Console.WriteLine("Permission exists.");
                    expires = Convert.ToInt32(DateTime.UtcNow.Subtract(BeginningOfTime).TotalSeconds) + expireInSeconds;
                    return new PermissionToken()
                    {
                        Token = per.Token,
                        Expires = expires ?? 0,
                        UserId = user.Id,
                        Id = per.Id,
                        ResourceId = per.ResourceLink,
                    };
                }
            }
            var result = Client.CreatePermissionAsync(user.SelfLink, p);
            result.Wait();
            Permission permission = result.Result;

            expires = Convert.ToInt32(DateTime.UtcNow.Subtract(BeginningOfTime).TotalSeconds) + expireInSeconds;
            return new PermissionToken()
            {
                Token = permission.Token,
                Expires = expires ?? 0,
                UserId = user.Id,
                Id = permission.Id,
                ResourceId = permission.ResourceLink,
            };
        } //~GetToken

        public static List<Permission> GetAllPermissions(User user, string databaseId)
        {
            string continuation = null;
            List<Permission> permissions = new List<Permission>();
            do
            {
                var task = Client.ReadPermissionFeedAsync(
                    UriFactory.CreateUserUri(databaseId, user.Id),
                    new FeedOptions
                    {
                        MaxItemCount = -1,
                        RequestContinuation = continuation,
                        MaxDegreeOfParallelism = -1
                    });
                task.Wait();
                continuation = task.Result.ResponseContinuation;

                List<Task> insertTasks = new List<Task>();
                foreach (Permission p in task.Result)
                {
                    permissions.Add(p);
                }
            } while (continuation != null);

            return permissions;
        }

        public void Delete(User user, Permission permission, string databaseId)
        {
            var permissionDeleteTask = Client.DeletePermissionAsync(UriFactory.CreatePermissionUri(
                 databaseId,
                 user.Id,
                 permission.Id));
            permissionDeleteTask.Wait();
        }

        public Permission Update(User user, Permission permission, string databaseId)
        {
            var permissionUpdateTask = Client.UpsertPermissionAsync(
                  UriFactory.CreateUserUri(databaseId, user.Id), permission);
            permissionUpdateTask.Wait();

            return permissionUpdateTask.Result;
        }

    } //Class

public class PermissionToken
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }
        [JsonProperty(PropertyName = "expires")]
        public int Expires { get; set; }
        [JsonProperty(PropertyName = "userid")]
        public string UserId { get; set; }
        [JsonProperty(PropertyName = "resourceId")]
        public string ResourceId { get; set; }
    }
}
