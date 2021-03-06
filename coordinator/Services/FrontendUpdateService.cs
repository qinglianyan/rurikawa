using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Karenia.Rurikawa.Helpers;
using Karenia.Rurikawa.Models.WebsocketApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Karenia.Rurikawa.Coordinator.Services {
    using FrontendWebsocketWrapperTy = JsonWebsocketWrapper<WsApiClientMsg, WsApiServerMsg>;

    public class FrontendConnection {
        public string Username { get; }
        public FrontendWebsocketWrapperTy Conn { get; }

        public FrontendConnection(FrontendWebsocketWrapperTy conn, string username) {
            Conn = conn;
            Username = username;
        }

        public SemaphoreSlim SubscriptionLock { get; } = new SemaphoreSlim(1);
        public Dictionary<FlowSnake, IDisposable> JobOutputSubscriptions { get; } = new Dictionary<FlowSnake, IDisposable>();
        public Dictionary<FlowSnake, IDisposable> JobSubscriptions { get; } = new Dictionary<FlowSnake, IDisposable>();
    }

    public class FrontendUpdateService {
        private readonly JsonSerializerOptions jsonSerializerOptions;
        private readonly IServiceScopeFactory scopeProvider;
        private readonly RedisService redis;
        private readonly ILogger<FrontendUpdateService> logger;
        private readonly ILogger<FrontendWebsocketWrapperTy> wsLogger;

        public FrontendUpdateService(
            JsonSerializerOptions jsonSerializerOptions,
            IServiceScopeFactory serviceProvider,
            RedisService redis,
            ILogger<FrontendUpdateService> logger,
            ILogger<FrontendWebsocketWrapperTy> wsLogger) {
            this.jsonSerializerOptions = jsonSerializerOptions;
            this.scopeProvider = serviceProvider;
            this.redis = redis;
            this.logger = logger;
            this.wsLogger = wsLogger;
        }

        /// <summary>
        /// This is basically a concurrent hashset, but since C# doesn't support ZST, 
        /// we use a dummy int as value.
        /// </summary>
        private readonly ConcurrentDictionary<FrontendConnection, int> connectionHandles = new ConcurrentDictionary<FrontendConnection, int>();

        private readonly ConcurrentDictionary<FlowSnake, (Subject<JobStatusUpdateMsg>, IObservable<JobStatusUpdateMsg>)> jobUpdateListeners =
            new ConcurrentDictionary<FlowSnake, (Subject<JobStatusUpdateMsg>, IObservable<JobStatusUpdateMsg>)>();

        /// <summary>
        /// Try to use the provided HTTP connection to create a WebSocket connection
        /// between coordinator and frontend. 
        /// </summary>
        /// <param name="ctx">
        ///     The provided connection. Must be upgradable into websocket.
        /// </param>
        /// <returns>
        ///     True if the websocket connection was made.
        /// </returns>
        public async ValueTask<bool> TryUseConnection(HttpContext ctx) {
            var scope = scopeProvider.CreateScope();
            var username = await Authorize(scope, ctx);

            if (username != null) {
                var ws = await ctx.WebSockets.AcceptWebSocketAsync();
                var wrapper = new FrontendWebsocketWrapperTy(
                    ws,
                    jsonSerializerOptions,
                    4096,
                    wsLogger);
                var conn = new FrontendConnection(wrapper, username);

                connectionHandles.TryAdd(conn, 0);

                try {
                    using var _ = SetupObservables(conn);
                    await conn.Conn.WaitUntilClose();
                } catch (Exception) {
                }


                connectionHandles.TryRemove(conn, out var _);
                await UnsubscribeAll(conn);

                return true;
            } else {
                ctx.Response.StatusCode = 401; // unauthorized
            }
            return false;
        }

        private ValueTask<string?> Authorize(
            IServiceScope scope,
            HttpContext ctx) {
            var account = scope.ServiceProvider.GetService<AccountService>();
            if (ctx.Request.Query.TryGetValue("token", out var tokens)) {
                var token = tokens.First();
                var username = account.VerifyShortLivingToken(token);
                return new ValueTask<string?>(username);
            } else {
                return new ValueTask<string?>((string?)null);
            }
        }

        private IDisposable SetupObservables(FrontendConnection conn) {
            conn.Conn.Messages.Connect();
            return conn.Conn.Messages.Subscribe((val) => {
                switch (val) {
                    case SubscribeMsg msg:
                        this.HandleSubscribeMsg(msg, conn);
                        break;
                    default:
                        logger.LogWarning("Unknown message: {0}", val);
                        break;
                }
            });
        }

        private async void HandleSubscribeMsg(SubscribeMsg msg, FrontendConnection conn) {
            using var lock_ = await conn.SubscriptionLock.LockAsync();
            if (msg.Sub) {
                if (msg.Jobs != null) {
                    foreach (var job in msg.Jobs) {
                        this.SubscribeToJob(job, conn);
                    }
                }
            } else {
                if (msg.Jobs != null) {
                    foreach (var job in msg.Jobs) {
                        this.UnsubscribeJob(job, conn);
                    }
                }
            }
        }

        public void UnsubscribeJob(FlowSnake id, FrontendConnection conn) {
            conn.JobSubscriptions.Remove(id, out var val);
            val?.Dispose();
        }

        public void SubscribeToJob(FlowSnake id, FrontendConnection conn) {
            if (conn.JobSubscriptions.ContainsKey(id)) return;
            var sub = jobUpdateListeners.GetOrAdd(
                id,
                _x => {
                    var subject = new Subject<JobStatusUpdateMsg>();
                    var res = new RefCountFusedObservable<JobStatusUpdateMsg>(
                    subject.ObserveOn(Scheduler.Default),
                    () => {
                        jobUpdateListeners.TryRemove(id, out _);
                    });
                    return (subject, res);
                });

            var subscripton = sub.Item2.Subscribe(async (msg) => {
                try {
                    await conn.Conn.SendMessage(msg);
                } catch (Exception e) { logger.LogError(e, "Failed to send message"); }
            });
            conn.JobSubscriptions.Add(id, subscripton);
        }

        // public async Task SubscribeToJobOutput(FlowSnake id, ConnectionMultiplexer redis, FrontendConnection connection) {
        //     var createGroupResult = await redis.GetDatabase().StreamCreateConsumerGroupAsync(
        //         JudgerCoordinatorService.FormatJobStdout(id),
        //         connection.Username,
        //         createStream: false,
        //         position: StreamPosition.Beginning);
        //     // redis.GetSubscriber().SubscribeAsync()
        // }

        public async ValueTask UnsubscribeAll(FrontendConnection conn) {
            using var locked = await conn.SubscriptionLock.LockAsync();
            foreach (var a in conn.JobOutputSubscriptions) {
                a.Value.Dispose();
            }
            foreach (var a in conn.JobSubscriptions) {
                a.Value.Dispose();
            }
        }

        public void OnJobStautsUpdate(FlowSnake id, JobStatusUpdateMsg msg) {
            if (jobUpdateListeners.TryGetValue(id, out var val)) {
                val.Item1.OnNext(msg);
            }
        }

        public async ValueTask ClearNotifications(FrontendConnection conn) {
            using (await conn.SubscriptionLock.LockAsync()) {
                foreach (var sub in conn.JobSubscriptions) {
                    sub.Value.Dispose();
                }
            }
        }
    }
}
