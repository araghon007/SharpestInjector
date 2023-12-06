using System.Collections.Generic;
using System.Diagnostics;
using SharpestInjector;
using System.Windows;
using System.Linq;
using System.IO;
using System;

namespace SharpestInjectorGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        public List<ProcessInfo> ProcessBindTest = new List<ProcessInfo>();

        private void InjectedProcesses_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                foreach(var filename in filenames)
                {
                    if (Path.GetExtension(filename).ToUpperInvariant() != ".DLL")
                        continue;

                    var peFile = PeFile.Parse(filename);
                    InjectDlls.Items.Add($"{filename} ({(peFile.Is64Bit?"64":"32")}-bit)");
                    DllList.Add(peFile);
                }
            }
        }

        List<PeFile> DllList = new List<PeFile>();

        private void InjectedProcesses_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                foreach (string filename in filenames)
                {
                    if (Path.GetExtension(filename).ToUpperInvariant() == ".DLL")
                    {
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        List<Process> ProcessList = new List<Process>();

        private void Refresh(object sender, RoutedEventArgs e)
        {
            ProcessList.Clear();
            ProcessBindTest.Clear();

            var strings = new List<string>();
            foreach(var process in Process.GetProcesses())
            {
                var proc = Injector.GetProcessInfo(process);

                if (proc.Modules.Count <= 0)
                    continue;

                if (string.IsNullOrWhiteSpace(ModuleFilter.Text) == false)
                {
                    bool found = false;
                    foreach(var module in proc.Modules)
                    {
                        if (module.Key.EndsWith(ModuleFilter.Text.ToUpperInvariant()))
                        {
                            found = true;
                            continue;
                        }
                    }

                    if(found == false)
                        continue;
                }

                if(proc.WindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                string fileName = Path.GetFileName(proc.Modules.First().Value.Path);
                proc.FileName = fileName;

                /*
                if(fileName == "WindowsProject1.exe")
                {
                    var hahpro = PeFile.Parse($@"{Environment.CurrentDirectory}\SharpestTestDLL64.dll");
                    
                    var hModule = Injector.Inject(proc); // TODO: Maybe make the injection use the same hack for retrieving parameters I have yet to develop, so we can get the 64-bit handle
                    var proce = Injector.GetProcessInfo(process);
                    //hModule = new IntPtr(0x00007ffffb930000);
                    hModule = proce.Modules[$@"{Environment.CurrentDirectory}\SharpestTestDLL64.dll".ToUpperInvariant()].MemoryAddress;

                    var helloWorld = IntPtr.Add(hModule, hahpro.GetExportAddress("HelloWorld"));
                    var helloWorld1 = IntPtr.Add(hModule, hahpro.GetExportAddress("HelloWorld1"));
                    var helloWorld2 = IntPtr.Add(hModule, hahpro.GetExportAddress("HelloWorld2"));
                    var helloWorld7 = IntPtr.Add(hModule, hahpro.GetExportAddress("HelloWorld7"));
                    var helloWorldL = IntPtr.Add(hModule, hahpro.GetExportAddress("HelloWorldL"));
                    var helloWorldAaaaa = IntPtr.Add(hModule, hahpro.GetExportAddress("?HelloWorldOutTest@@YAKPEB_W@Z"));
                    /*                    
                    var aee1 = Injector.TestParams64(proc, helloWorld);
                    var aee2 = Injector.TestParams64(proc, helloWorld1, "TestB");
                    var aee3 = Injector.TestParams64(proc, helloWorld2, "TestC", "Test3");
                    var aee4 = Injector.TestParams64(proc, helloWorld7, "TestD", "Test4", "A", "B", "C", "D", "E");
                    var aee5 = Injector.TestParams64(proc, helloWorldL, "The Thing", 42, 6.21f);
                    */
                /*
                    var aaaa = Injector.TestParams64(proc, helloWorldAaaaa, "The Thing");

                    var success = Injector.Unload(proc);
                }
                */
                ProcessBindTest.Add(proc);
                ProcessList.Add(process);
            }
            //strings.Sort();
            /*
            foreach(var str in strings)
            {
                Processes.Items.Add(str);
            }
            */
            ProcessBindTest = ProcessBindTest.OrderBy(x => x.ToString()).ToList();
            Processes.ItemsSource = ProcessBindTest;
        }

        private void Inject(object sender, RoutedEventArgs e)
        {
            var selected = Processes.SelectedItem as ProcessInfo;

            if (selected == null && DllList.Count == 0)
                return;

            foreach(var dll in DllList)
            {
                if (dll.Is64Bit == selected.Is64Bit)
                {
                    var succ = Injector.Inject(selected, dll);
                }
            }
        }

        private void Unload(object sender, RoutedEventArgs e)
        {
            var selected = Processes.SelectedItem as ProcessInfo;

            if (selected == null && DllList.Count == 0)
                return;


            foreach (var dll in DllList)
            {
                if (dll.Is64Bit == selected.Is64Bit)
                {
                    var succ = Injector.Unload(selected, dll);
                }
            }
        }

        private void CallExport(object sender, RoutedEventArgs e)
        {
            var selected = Processes.SelectedItem as ProcessInfo;

            if (selected == null)
                return;

            var exportWindow = new CallExport(selected);
            exportWindow.Show();
        }
    }
}
