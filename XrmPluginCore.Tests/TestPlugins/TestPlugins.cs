using XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using System;

namespace XrmPluginCore.Tests.TestPlugins
{
    public class TestAccountPlugin : Plugin
    {
        public bool ExecutedAction { get; private set; }
        public LocalPluginContext LastContext { get; private set; }
        public IServiceProvider LastProvider { get; private set; }

        public TestAccountPlugin()
        {
            RegisterPluginStep<TestAccount>(EventOperation.Create, ExecutionStage.PreOperation, ExecuteCtx);
            RegisterStep<TestAccount>(EventOperation.Create, ExecutionStage.PostOperation, ExecuteSP);
        }

        private void ExecuteCtx(LocalPluginContext context)
        {
            ExecutedAction = true;
            LastContext = context;
        }

        private void ExecuteSP(IServiceProvider context)
        {
            ExecutedAction = true;
            LastProvider = context;
        }
    }

    public class TestCustomMessagePlugin : Plugin
    {
        public bool ExecutedAction { get; private set; }
        public LocalPluginContext LastContext { get; private set; }

        public TestCustomMessagePlugin()
        {
            RegisterPluginStep("custom_message", EventOperation.Execute, ExecutionStage.PreOperation, Execute);
        }

        private void Execute(LocalPluginContext context)
        {
            ExecutedAction = true;
            LastContext = context;
        }
    }

    public class TestNoRegistrationPlugin : Plugin
    {
        public bool ExecutedAction { get; private set; }

        // No registrations added
    }

    public class TestMultipleRegistrationPlugin : Plugin
    {
        public bool CreateExecuted { get; private set; }
        public bool UpdateExecuted { get; private set; }

        public TestMultipleRegistrationPlugin()
        {
            RegisterPluginStep<TestAccount>(EventOperation.Create, ExecutionStage.PreOperation, OnCreate);
            RegisterPluginStep<TestAccount>(EventOperation.Update, ExecutionStage.PostOperation, OnUpdate);
        }

        private void OnCreate(LocalPluginContext context)
        {
            CreateExecuted = true;
        }

        private void OnUpdate(LocalPluginContext context)
        {
            UpdateExecuted = true;
        }
    }

    // Simple entity class for testing
    public class TestAccount : Entity
    {
        public TestAccount() : base("account") { }
    }
}