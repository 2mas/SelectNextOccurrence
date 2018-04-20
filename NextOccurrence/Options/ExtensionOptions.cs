using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace NextOccurrence.Options
{
    internal class ExtensionOptions : DialogPage
    {
        private static ExtensionOptions instance;

        /// <summary>
        /// The name of the options collection as stored in the registry.
        /// </summary>
        private string CollectionName { get; } = typeof(ExtensionOptions).FullName;

        public ExtensionOptions() { }

        internal static ExtensionOptions Instance
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (instance == null)
                {
                    instance = new ExtensionOptions();
                }

                instance.LoadSettingsFromStorage();
                return instance;
            }
        }

        [Category(Vsix.Name)]
        [DisplayName("Add cursors by mouse-clicking")]
        [Description("Activate adding cursors by pressing modifier + left mousebutton click")]
        [DefaultValue(true)]
        [ExtensionSetting]
        public bool AddMouseCursors { get; set; } = true;

        public override void SaveSettingsToStorage()
        {
            SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            WritableSettingsStore settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(CollectionName))
            {
                settingsStore.CreateCollection(CollectionName);
            }

            foreach (PropertyInfo property in GetOptionProperties())
            {
                string output = SerializeValue(property.GetValue(this));
                settingsStore.SetString(CollectionName, property.Name, output);
            }

            LoadSettingsFromStorage();
        }

        public override void LoadSettingsFromStorage()
        {
            SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            SettingsStore settingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(CollectionName))
            {
                return;
            }

            foreach (PropertyInfo property in GetOptionProperties())
            {
                try
                {
                    string serializedProp = settingsStore.GetString(CollectionName, property.Name);
                    object value = DeserializeValue(serializedProp, property.PropertyType);
                    property.SetValue(this, value);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(ex);
                }
            }
        }

        private IEnumerable<PropertyInfo> GetOptionProperties()
        {
            return GetType()
                .GetProperties()
                .Where(
                    p => p.PropertyType.IsSerializable
                    && p.PropertyType.IsPublic
                    && Attribute.IsDefined(
                        p, typeof(ExtensionSettingAttribute)
                    )
                );
        }

        /// <summary>
        /// Serializes an object value to a string using the binary serializer.
        /// </summary>
        protected virtual string SerializeValue(object value)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, value);
                stream.Flush();
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        /// <summary>
        /// Deserializes a string to an object using the binary serializer.
        /// </summary>
        protected virtual object DeserializeValue(string value, Type type)
        {
            byte[] b = Convert.FromBase64String(value);

            using (var stream = new MemoryStream(b))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(stream);
            }
        }
    }
}
