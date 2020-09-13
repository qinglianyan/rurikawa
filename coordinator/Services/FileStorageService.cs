using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Karenia.Rurikawa.Helpers;
using Microsoft.Extensions.Logging;

namespace Karenia.Rurikawa.Coordinator.Services {
    public class SingleBucketFileStorageService {
        public class Params {
            public string Bucket { get; set; }
            public string Endpoint { get; set; }
            public string? PublicEndpoint { get; set; }
            public string AccessKey { get; set; }
            public string SecretKey { get; set; } = "";
            public string BucketPolicy { get; set; } = "";
            public bool Ssl { get; set; } = true;
            public bool PublicSsl { get; set; } = true;
        }

        public SingleBucketFileStorageService(
            Params param,
            ILogger<SingleBucketFileStorageService> logger
        ) : this(
            param.Bucket,
            param.Endpoint,
            param.PublicEndpoint,
            param.AccessKey,
            param.SecretKey,
            param.Ssl,
            param.PublicSsl,
            logger
        ) { }

        public SingleBucketFileStorageService(
            string bucket,
            string endpoint,
            string? publicEndpoint,
            string accessKey,
            string secretKey,
            bool hasSsl,
            bool hasPublicSsl,
            ILogger<SingleBucketFileStorageService> logger
        ) {
            client = new Minio.MinioClient(endpoint, accessKey, secretKey);
            if (hasSsl) client = client.WithSSL();
            this.bucket = bucket;
            this.endpoint = endpoint;
            this.publicEndpoint = publicEndpoint;
            var endpointUri = new UriBuilder(publicEndpoint ?? this.endpoint);
            if (endpointUri.Host == null || endpointUri.Host == "") {
            } else {
                endpointUri.Scheme = hasPublicSsl ? "https" : "http";
            }
            this.publicEndpointUri = new Uri(endpointUri.Uri, bucket);
            this.hasSsl = hasSsl;
            this.logger = logger;
        }

        private ILogger<SingleBucketFileStorageService> logger;

        private Minio.MinioClient client;
        private readonly string bucket;
        private readonly string endpoint;
        private readonly string? publicEndpoint;
        private readonly bool hasSsl;
        private readonly Uri publicEndpointUri;

        public async Task Check() {
            if (!await client.BucketExistsAsync(bucket)) {
                await client.MakeBucketAsync(bucket);
                await client.SetPolicyAsync(bucket, $@"{{
                ""Version"":""2012-10-17"",
                ""Statement"":[
                    {{
                    ""Sid"":""PublicRead"",
                    ""Effect"":""Allow"",
                    ""Principal"": ""*"",
                    ""Action"":[""s3:GetObject"", ""s3:GetObjectVersion""],
                    ""Resource"":[""arn:aws:s3:::{bucket}:/*""]
                    }}
                ]
            }}
            ");
            }
        }

        public async Task UploadFile(
            string fileName,
            Stream file,
            long length,
            bool isPublic = true,
            CancellationToken c = default
        ) {
            logger.LogInformation("Upload started. filename {0}, length {1}", fileName, length);
            var metadata = new Dictionary<string, string>();
            if (isPublic) {
                metadata["x-amz-acl"] = "public-read";
            }
            await client.PutObjectAsync(
                bucket,
                fileName,
                file,
                length,
                metaData: metadata,
                cancellationToken: c);
            logger.LogInformation("Upload end.");
        }

        /// <summary>
        /// Formats and returns the 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string GetFileAddress(
            string filename
        ) {
            var uri = new Uri(publicEndpointUri, filename);
            return uri.ToString();
        }
    }
}