using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace EmailPaySlipCdk
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var config = new Config();
            Configuration.Bind(config);
            
            var app = new App();
            new EmailPaySlipCdkStack(app, "EmailPaySlipCdkStack", new StackProps
            {
                // If you don't specify 'env', this stack will be environment-agnostic.
                // Account/Region-dependent features and context lookups will not work,
                // but a single synthesized template can be deployed anywhere.

                // Uncomment the next block to specialize this stack for the AWS Account
                // and Region that are implied by the current CLI configuration.
                /*
                Env = new Amazon.CDK.Environment
                {
                    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
                }
                */

                // Uncomment the next block if you know exactly what Account and Region you
                // want to deploy the stack to.
                /*
                Env = new Amazon.CDK.Environment
                {
                    Account = "123456789012",
                    Region = "us-east-1",
                }
                */

                // For more information, see https://docs.aws.amazon.com/cdk/latest/guide/environments.html
            }, config);
            app.Synth();
        }
    }

    public class EmailAddress
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class BucketKey
    {
        public string Bucket { get; set; }
        public string Key { get; set; }
    }
    
    public class Config
    {
        public string[] EmailIdentities { get; set; }
        public EmailAddress FromAddress { get; set; }
        public EmailAddress ToAddress { get; set; }
        public BucketKey SavePDF { get; set; }
    }
}
