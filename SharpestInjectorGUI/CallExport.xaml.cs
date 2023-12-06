using System.Collections.Generic;
using System.Windows.Controls;
using SharpestInjector;
using System.Windows;
using System.Linq;

namespace SharpestInjectorGUI
{
    /// <summary>
    /// Interaction logic for CallExport.xaml
    /// </summary>
    public partial class CallExport : Window
    {
        private ProcessInfo Process;

        public CallExport()
        {
            InitializeComponent();
        }

        public CallExport(ProcessInfo process)
        {
            InitializeComponent();

            Process = process;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var modules = new List<ModuleInfo>();
            foreach(var module in Process.Modules)
            {
                modules.Add(module.Value);
            }
            var mods = modules.OrderBy(x => x.Path);
            ProcessModules.ItemsSource = mods;
        }

        private void ProcessModules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var module = ProcessModules.SelectedItem as ModuleInfo;
            if (module == null)
                return;

            var peFile = PeFile.Parse(module.Path);

            var exports = new List<ModuleExport>();
            foreach(var export in peFile.Exports)
            {
                exports.Add(new ModuleExport()
                {
                    Name = export.Key,
                    Address = export.Value
                });
            }

            exports.OrderBy(s => s.Name);


            ModuleExports.ItemsSource = exports;
        }

        private void ModuleExports_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    public class ModuleExport
    {
        public string Name { get; set; }
        public int Address { get; set; }

        override public string ToString()
        {
            return Name;
        }
    }
}
