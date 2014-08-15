﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using FluentAssertions;
using JimBobBennett.JimLib.Events;
using JimBobBennett.JimLib.Mvvm;
using NUnit.Framework;

namespace JimBobBennett.JimLib.Test.Mvvm
{
    [TestFixture]
    public class ViewModelBaseTest
    {
        class MyModel : NotificationObject
        {
            private string _first;
            private int _second;

            public string First
            {
                get { return _first; }
                set
                {
                    if (value == _first) return;
                    _first = value;
                    RaisePropertyChanged();
                }
            }

            public int Second
            {
                get { return _second; }
                set
                {
                    if (value == _second) return;
                    _second = value;
                    RaisePropertyChanged();
                }
            }
        }

        class MyViewModel : ViewModelBase<MyModel>
        {
            public MyViewModel(MyModel model) : base(model)
            {
                Models = new List<MyModel>();
                RaisedModelPropertyChanges = new List<string>();
            }

            public MyViewModel()
            {
                Models = new List<MyModel>();
                RaisedModelPropertyChanges = new List<string>();
            }

            protected override void OnModelChanged(MyModel oldModel, MyModel newModel)
            {
                base.OnModelChanged(oldModel, newModel);

                if (Models != null)
                {
                    Models.Add(oldModel);
                    Models.Add(newModel);
                }
            }

            protected override void OnModelPropertyChanged(string propertyName)
            {
                base.OnModelPropertyChanged(propertyName);

                RaisedModelPropertyChanges.Add(propertyName);
            }

            public string First
            {
                get { return Model.First; }
                set { Model.First = value; }
            }

            public List<MyModel> Models { get; private set; }
            public List<string> RaisedModelPropertyChanges { get; set; }
        }

        class MyNonGenericViewModel : ViewModelBase
        {
            public MyNonGenericViewModel(object model) : base(model)
            {
            }

            public MyNonGenericViewModel()
            {
            }
        }

        public class MyEventArgs : EventArgs
        {
            
        }

        public class ViewModelWithEvents : ViewModelBase
        {
            public event EventHandler EventWithNoArgs;
            public event EventHandler<MyEventArgs> EventWithBasicArgs;
            public event EventHandler<EventArgs<string>> EventWithStringArgs;

            public void FireEventWithNoArgs()
            {
                FireEvent(EventWithNoArgs);
            }

            public void FireEventWithBasicArgs()
            {
                FireEvent(EventWithBasicArgs, new MyEventArgs());
            }

            public void FireEventWithStringArgs(string value)
            {
                FireEvent(EventWithStringArgs, new EventArgs<string>(value));
            }
        }

        [Test]
        public void SettingModelCallsOnModelUpdated()
        {
            var model1 = new MyModel();
            var model2 = new MyModel();

// ReSharper disable once UseObjectOrCollectionInitializer
            var vm = new MyViewModel(model1);
            vm.Model = model2;

            vm.Models[0].Should().Be(model1);
            vm.Models[1].Should().Be(model2);
        }

        [Test]
        public void PropertyChangeForPassthroughIsRaised()
        {
            var model = new MyModel();
            var vm = new MyViewModel(model);

            model.MonitorEvents();
            vm.MonitorEvents();

            vm.First = "Hello";

            model.ShouldRaisePropertyChangeFor(m => m.First);
            vm.ShouldRaisePropertyChangeFor(v => v.First);
        }

        [Test]
        public void PropertyChangeOnModelForPassthroughIsRaised()
        {
            var model = new MyModel();
            var vm = new MyViewModel(model);

            model.MonitorEvents();
            vm.MonitorEvents();

            model.First = "Hello";

            model.ShouldRaisePropertyChangeFor(m => m.First);
            vm.ShouldRaisePropertyChangeFor(v => v.First);
        }

        [Test]
        public void PropertyChangeForNonPassthroughIsNotRaised()
        {
            var model = new MyModel();
            var vm = new MyViewModel(model);

            vm.MonitorEvents();

            model.Second = 100;

            vm.ShouldNotRaise("PropertyChanged");
        }

        [Test]
        public void ChangingTheModelRaisesAPropertyChangeForAll()
        {
            var model = new MyModel();
            var vm = new MyViewModel(model);
            vm.MonitorEvents();

            vm.Model = new MyModel();

            vm.ShouldRaise("PropertyChanged").WithArgs<PropertyChangedEventArgs>(e => e.PropertyName == string.Empty);
        }

        [Test]
        public void ChangingTheModelUnsubscribesFromPropertyChangesOnTheOldModel()
        {
            var model = new MyModel();
            var vm = new MyViewModel(model);

            vm.Model = new MyModel();

            vm.MonitorEvents();

            model.First = "Hello";

            vm.ShouldNotRaise("PropertyChanged");
        }

        [Test]
        public void OnModelPropertyChangeForPassthroughIsCalled()
        {
            var model = new MyModel();
            var vm = new MyViewModel(model);
            
            vm.First = "Hello";

            vm.RaisedModelPropertyChanges.Should().OnlyContain(s => s == "First");
        }

        [Test]
        public void FireEventFiresEventWithNoArgs()
        {
            var vm = new ViewModelWithEvents();
            vm.MonitorEvents();
            vm.FireEventWithNoArgs();
            vm.ShouldRaise("EventWithNoArgs").WithArgs<EventArgs>(a => a == EventArgs.Empty);
        }

        [Test]
        public void FireEventFiresEventWithBasicArgs()
        {
            var vm = new ViewModelWithEvents();
            vm.MonitorEvents();
            vm.FireEventWithBasicArgs();
            vm.ShouldRaise("EventWithBasicArgs").WithArgs<MyEventArgs>(a => true);
        }

        [Test]
        public void FireEventFiresEventWithPArameterisedArgs()
        {
            var vm = new ViewModelWithEvents();
            vm.MonitorEvents();
            vm.FireEventWithStringArgs("HelloWorld");
            vm.ShouldRaise("EventWithStringArgs").WithArgs<EventArgs<string>>(a => a.Value == "HelloWorld");
        }

        [Test]
        public async Task BusyIndicatorIsToggledAroundRunAction()
        {
            var vm = new ViewModelWithEvents();

            var busyStates = new List<bool> {vm.IsBusy};

            await vm.RunWithBusyIndicatorAsync(() => busyStates.Add(vm.IsBusy));
            busyStates.Add(vm.IsBusy);

            busyStates.Should().HaveCount(3);
            busyStates.Should().ContainInOrder(new List<bool>
            {
                false,
                true,
                false
            });
        }

        [Test]
        public async Task BusyIndicatorRaisesPropertyChangeBeforeRunAction()
        {
            var vm = new ViewModelWithEvents();

            vm.MonitorEvents();

            await vm.RunWithBusyIndicatorAsync(() => vm.ShouldRaisePropertyChangeFor(v => v.IsBusy));
        }

        [Test]
        public async Task BusyIndicatorRaisesPropertyChangeAfterRunAction()
        {
            var vm = new ViewModelWithEvents();

            await vm.RunWithBusyIndicatorAsync(vm.MonitorEvents);
            vm.ShouldRaisePropertyChangeFor(v => v.IsBusy);
        }

        [Test]
        public void IsBusyDoesntRaisePropertyChangeIfTheValueDoesntChange()
        {
            var vm = new ViewModelWithEvents();
            vm.MonitorEvents();
            vm.IsBusy = false;
            vm.ShouldNotRaisePropertyChangeFor(v => v.IsBusy);
        }

        [Test]
        public void ModelIsSetOnConstructorForNonGeneric()
        {
            var model = new List<string>();
            var vm = new MyNonGenericViewModel(model);
            vm.Model.Should().Be(model);
        }

        [Test]
        public void ModelIsSetToNullIfNotPassedToConstructorForNonGeneric()
        {
            var vm = new MyNonGenericViewModel();
            vm.Model.Should().BeNull();
        }

        [Test]
        public void ModelIsSetToNullIfNotPassedToConstructorForGeneric()
        {
            var vm = new MyViewModel();
            vm.Model.Should().BeNull();
        }
    }
}
