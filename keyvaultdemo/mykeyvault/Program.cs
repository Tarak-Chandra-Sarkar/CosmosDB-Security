using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Configuration;
using System.Threading.Tasks;


namespace mykeyvault
{
    class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }
        public static async Task MainAsync(string[] args)
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

            try
            {
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var secret = await keyVaultClient.GetSecretAsync(ConfigurationManager.AppSettings["CosmosDBMasterKey"])  
                    .ConfigureAwait(false);

                Console.WriteLine( $"Secret: {secret.Value}");

            }
            catch (Exception exp)
            {
                Console.WriteLine( $"Something went wrong: {exp.Message}");
            }

            Console.WriteLine(azureServiceTokenProvider.PrincipalUsed != null ? $"Principal Used: {azureServiceTokenProvider.PrincipalUsed}" : string.Empty);
            Console.Read();
        }
    } //~class
} //~nameSpace

//How to run this application using a service principal https://azure.microsoft.com/en-us/resources/samples/app-service-msi-keyvault-dotnet/
