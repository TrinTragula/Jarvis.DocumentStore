﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.QuartzIntegration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Jarvis.ImageService.Core.Jobs;
using Jarvis.ImageService.Core.ProcessingPipeline;
using Jarvis.ImageService.Core.ProcessingPipeline.Pdf;
using Jarvis.ImageService.Core.Services;
using Quartz;
using Quartz.Impl.MongoDB;

namespace Jarvis.ImageService.Core.Support
{
    public class SchedulerInstaller : IWindsorInstaller
    {
        public SchedulerInstaller(string jobStoreConnectionString)
        {
            JobStore.DefaultConnectionString = jobStoreConnectionString;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.AddFacility<CustomQuartzFacility>(c => c.Configure(CreateDefaultConfiguration()));

            container.Register(
                Classes
                    .FromAssemblyInThisApplication()
                    .BasedOn<IJob>()
                    .WithServiceSelf()
                    .LifestyleTransient(),
                Component
                    .For<CreateImageFromPdfTask>()
                    .LifestyleTransient(),
                Component
                    .For<IPipelineScheduler>()
                    .ImplementedBy<PipelineScheduler>()
            );

            container.Resolve<IScheduler>().ListenerManager.AddJobListener(new JobsListener(
                container.Resolve<ILogger>(),
                container.Resolve<IPipelineScheduler>(),
                container.Resolve<IFileService>()
            ));
        }

        IConfiguration CreateDefaultConfiguration()
        {
            var config = new MutableConfiguration("scheduler_config");
            var quartz = new MutableConfiguration("quartz");
            config.Children.Add(quartz);

            quartz.CreateChild("item", "Scheduler")
                    .Attribute("key", "quartz.scheduler.instanceName");

            quartz.CreateChild("item", "Quartz.Simpl.SimpleThreadPool, Quartz")
                    .Attribute("key", "quartz.threadPool.type");

            quartz.CreateChild("item", "5")
                    .Attribute("key", "quartz.threadPool.threadCount");

            quartz.CreateChild("item", "Normal")
                    .Attribute("key", "quartz.threadPool.threadPriority");

            quartz.CreateChild("item", "Quartz.Impl.MongoDB.JobStore, Quartz.Impl.MongoDB")
                    .Attribute("key", "quartz.jobStore.type");

            return config;
        }
    }
}