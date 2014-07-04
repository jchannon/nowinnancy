namespace NowinNancy
{
    using System;
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    using Microsoft.Owin.Hosting;
    using Nancy;
    using Nancy.Bootstrapper;
    using Nancy.ErrorHandling;
    using Owin;

    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    using MidFunc = System.Func<
       System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>,
       System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>
       >;

    class Program
    {
        static void Main(string[] args)
        {
            var options = new StartOptions
            {
                ServerFactory = "Nowin",
                Port = 8080
            };

            using (WebApp.Start<Startup>(options))
            {
                Console.WriteLine("Running a http server on port 8080");
                Console.ReadKey();
            }
        }
    }

    public static class MyMiddleware
    {
        public static MidFunc DoIt()
        {
            return next => async env =>
            {
                try
                {
                    await next(env);
                }
                //catch (Exception exception)
                //{
                //   Debug.WriteLine(exception.Message);
                //}
                finally
                {
                    Debug.WriteLine(env["owin.ResponseStatusCode"]);
                }
            };
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app
                .Use(MyMiddleware.DoIt())
                .UseNancy();
        }
    }

    public class Module : NancyModule
    {
        public Module()
        {
            Get["/"] = _ => { throw new Exception("Oops"); };
        }
    }

    public class BoobStrapper : DefaultNancyBootstrapper
    {
        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration.WithOverrides(config =>
                    config.StatusCodeHandlers = new[] { typeof(RethrowStatusCodeHandler) });
            }
        }
    }

    public class RethrowStatusCodeHandler : IStatusCodeHandler
    {
        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return true;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            //Nancy catches exceptions from routes by default so we use this to bubble it back up to OWIN
            Exception innerException = ((Exception)context.Items[NancyEngine.ERROR_EXCEPTION]).InnerException;
            ExceptionDispatchInfo
                .Capture(innerException)
                .Throw();
        }
    }
}
