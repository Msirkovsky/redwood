﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Redwood.Framework.ViewModel;

namespace Redwood.Samples.BasicSamples.ViewModels
{
    public class Sample6ViewModel : RedwoodViewModelBase
    {
        [ViewModelProtection(ViewModelProtectionSettings.EnryptData)]
        public string SecretData { get; set; }

        [ViewModelProtection(ViewModelProtectionSettings.SignData)]
        public string ReadOnlyData { get; set; }


        public string NewTaskTitle { get; set; }

        public List<ProtectionTaskViewModel> ProtectedTasks { get; set; }

        public Sample6ViewModel()
        {
            ProtectedTasks = new List<ProtectionTaskViewModel>();
        }


        public override Task Init()
        {
            if (!Context.IsPostBack)
            {
                ReadOnlyData = "This property can be read on the client but when you modify it, the server won't accept the request.";
                SecretData = "This is encrypted and cannot be displayed on the client.";
                ProtectedTasks.Add(new ProtectionTaskViewModel() { IsCompleted = false, TaskId = Guid.NewGuid(), Title = "Do the laundry" });
                ProtectedTasks.Add(new ProtectionTaskViewModel() { IsCompleted = true, TaskId = Guid.NewGuid(), Title = "Wash the car" });
                ProtectedTasks.Add(new ProtectionTaskViewModel() { IsCompleted = true, TaskId = Guid.NewGuid(), Title = "Go shopping" });
            }
            return base.Init();
        }

        public void AddTask()
        {
            // ValidateSecretDataDecryptedSuccessfully();

            ProtectedTasks.Add(new ProtectionTaskViewModel()
            {
                Title = NewTaskTitle,
                TaskId = Guid.NewGuid()
            });
            NewTaskTitle = string.Empty;
        }

        public void CompleteTask(Guid id)
        {
            // ValidateSecretDataDecryptedSuccessfully();

            ProtectedTasks.Single(t => t.TaskId == id).IsCompleted = true;
        }



        private void ValidateSecretDataDecryptedSuccessfully()
        {
            if (SecretData != "This is encrypted and cannot be displayed on the client.")
            {
                throw new Exception("The secret data was not decrypted correctly!");
            }
        }

    }

    public class ProtectionTaskViewModel
    {

        public Guid TaskId { get; set; }

        public string Title { get; set; }

        public bool IsCompleted { get; set; }

        [ViewModelProtection(ViewModelProtectionSettings.EnryptData)]
        public string SecretTaskData { get; set; }

        public ProtectionTaskViewModel()
        {
            SecretTaskData = "secret task data";
        }
    }
}