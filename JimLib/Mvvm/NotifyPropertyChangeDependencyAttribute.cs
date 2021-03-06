﻿using System;

namespace JimBobBennett.JimLib.Mvvm
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class NotifyPropertyChangeDependencyAttribute : Attribute
    {
        public NotifyPropertyChangeDependencyAttribute(string dependentPropertyName)
        {
            DependentPropertyName = dependentPropertyName;
        }

        public string DependentPropertyName { get; set; }
    }
}
