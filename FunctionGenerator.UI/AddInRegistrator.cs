using FlexibleHMI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionGenerator.UI
{
    public class AddInRegistrator : IAddInRegistrator
    {
        public void RegisterAddIn(IServiceCollection services)
        {
            string key = "functionGenerator";
            services.AddKeyedTransient<TreeItemOption, FunctionGeneratorCreator>(key);
            services.AddKeyedTransient<TreeItemNode, FunctionGeneratorViewModel>(key);

            //services.AddTransient<FunctionGeneratorView>();
        }

        public void InitializeAddIn(ServiceProvider serviceProvider, IDataTemplateManager dataTemplateManager)
        {
            dataTemplateManager.RegisterDataTemplate<FunctionGeneratorViewModel, FunctionGeneratorView>();
        }
    }
}
