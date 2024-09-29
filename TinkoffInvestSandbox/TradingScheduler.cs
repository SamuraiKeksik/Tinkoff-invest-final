using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using TinkoffInvestSandbox;

namespace TinkoffInvestLibSandbox
{
    internal class TradingScheduler
    {
        public static async void Start()
        {
            IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            await scheduler.Start();

            IJobDetail job = JobBuilder.Create<Traiding>().Build();

            var startTime = DateTime.Parse($"{DateTime.Now.Hour + 1}:05");  //время начала бота назначается в следующий час и 5 минут
            if (DateTime.Now.Hour == 23) {startTime.AddDays(1);}  //Если бота запустили в 23 часа, то день запуска - завтра
            ITrigger trigger = TriggerBuilder.Create()  // создаем триггер
                .WithIdentity("trigger1", "group1")  // идентифицируем триггер с именем и группой
                .StartAt(startTime) // запуск в 10:00
                //.StartNow()
                .WithSimpleSchedule(x => x              // настраиваем выполнение действия
                    .WithIntervalInHours(1)             // через 1 час
                    .RepeatForever())                   // бесконечное повторение
                .Build();                               // создаем триггер

            await scheduler.ScheduleJob(job, trigger);        // начинаем выполнение работы
        }
    }
}
