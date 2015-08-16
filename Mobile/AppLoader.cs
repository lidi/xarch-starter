﻿using Common;
using Definition.Enums;
using Definition.Interfaces;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using Microsoft.Practices.ServiceLocation;
using Mobile.Stack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mobile
{
    public class AppLoader : IAppLoader
    {
        private static AsyncLock _lock = new AsyncLock();
        private IDictionary<StackEnum, IStack> _stacks = null;
        private StackEnum? _currentStack = null; 

        public AppLoader()
        {
            // Load Navigation Stacks
            InitializeStacks();                     
        }

        /// <summary>
        /// Loads all the navigation stacks
        /// </summary>
        private void InitializeStacks()
        {
            _stacks = new Dictionary<StackEnum, IStack>();
            _stacks.Add(StackEnum.Authentication, new AuthenticationStack());
            _stacks.Add(StackEnum.Main, new MainStack());
        }
        
        /// <summary>
        /// Automatically create an instance of and register any ViewModel found in the namespace
        /// Mobile.ViewModel
        /// </summary>
        public void InitializeViewModels()
        {
            string @namespace = "Mobile.ViewModel";

            var query = from t in typeof(App).GetTypeInfo().Assembly.DefinedTypes
                        where t.IsClass && t.Namespace == @namespace && t.Name != "BaseViewModel"
                        select t;

            foreach (var t in query.ToList())
            {
                var defaultConstructor = t.DeclaredConstructors.First();

                if (defaultConstructor.GetParameters().Count() == 0)
                    SimpleIoc.Default.Register(() => Activator.CreateInstance(t.AsType()), t.ToString());
                else
                {
                    List<object> parameters = new List<object>();
                    foreach (var p in defaultConstructor.GetParameters())
                    {
                        parameters.Add(ServiceLocator.Current.GetInstance(p.ParameterType));
                    }

                    SimpleIoc.Default.Register(() => Activator.CreateInstance(t.AsType(), parameters.ToArray()), t.ToString());
                }
            }
                

        }


        /// <summary>
        /// Changes the MainPage of the mobile app to the chosen stack
        /// </summary>
        /// <param name="stack"></param>
        public async Task LoadStack(StackEnum stack)
        {
            using (var releaser = await _lock.LockAsync())
            {
                if (stack == _currentStack)
                    return;

                // Unregister all current services
                UnRegisterServices();

                var stackInstance = _stacks[stack];

                // Register Services
                await stackInstance.RegisterServices();

                // Change MainPage
                App.Current.MainPage = stackInstance.MainPage;

                _currentStack = stack;
            }
        }
              
        /// <summary>
        /// Unregisters all services registered within this class
        /// </summary>
        private void UnRegisterServices()
        {
            SimpleIoc.Default.Unregister<IExtNavigationService>();
            SimpleIoc.Default.Unregister<IDialogService>();
        }
    }
}
