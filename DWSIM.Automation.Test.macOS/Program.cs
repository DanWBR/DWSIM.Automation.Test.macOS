using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DWSIM.Automation.Test.macOS
{

    class Program
    {

        // replace with DWSIM's installation directory on your computer
        static string DWSIMLibrariesDir = "/Users/Daniel/Desktop/DWSIM.app/Contents/MonoBundle/";

        static void Main()
        {

            // sets the assembly resolver to find remaining DWSIM libraries on demand
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

            System.IO.Directory.SetCurrentDirectory(DWSIMLibrariesDir);

            //create automation manager
            var interf = new DWSIM.Automation.Automation2();

            DWSIM.Interfaces.IFlowsheet sim;

            //load empty template simulation file
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "simulation_template.dwxml");

            sim = interf.LoadFlowsheet(fileName);

            var c1 = sim.AddObject(DWSIM.Interfaces.Enums.GraphicObjects.ObjectType.Cooler, 100, 100, "COOLER-001");
            var e1 = sim.AddObject(DWSIM.Interfaces.Enums.GraphicObjects.ObjectType.EnergyStream, 130, 150, "HEAT_OUT");
            var m1 = sim.AddObject(DWSIM.Interfaces.Enums.GraphicObjects.ObjectType.MaterialStream, 50, 100, "INLET");
            var m2 = sim.AddObject(DWSIM.Interfaces.Enums.GraphicObjects.ObjectType.MaterialStream, 150, 100, "OUTLET");

            // create the graphic object connectors manually as they are not being drawn on screen. 

            ((dynamic)c1.GraphicObject).PositionConnectors();
            ((dynamic)m1.GraphicObject).PositionConnectors();
            ((dynamic)m2.GraphicObject).PositionConnectors();
            ((dynamic)e1.GraphicObject).PositionConnectors();

            // connect the graphic objects.

            sim.ConnectObjects(m1.GraphicObject, c1.GraphicObject, 0, 0);
            sim.ConnectObjects(c1.GraphicObject, m2.GraphicObject, 0, 0);
            sim.ConnectObjects(c1.GraphicObject, e1.GraphicObject, 0, 0);

            // create and add an instance of PR Property Package

            var pr = new DWSIM.Thermodynamics.PropertyPackages.PengRobinsonPropertyPackage();
            pr.ComponentName = "Peng-Robinson (PR)";
            pr.ComponentDescription = "Peng-Robinson Property Package"; // <-- important to set any text as description.

            sim.AddPropertyPackage(pr);

            m1.PropertyPackage = sim.PropertyPackages.Values.First();
            m2.PropertyPackage = sim.PropertyPackages.Values.First();
            c1.PropertyPackage = sim.PropertyPackages.Values.First();

            // set inlet stream temperature
            // default properties: T = 298.15 K, P = 101325 Pa, Mass Flow = 1 kg/s

            var ms1 = (DWSIM.Thermodynamics.Streams.MaterialStream)m1;

            ms1.SetTemperature(400); // K

            // set cooler outlet temperature

            var cooler = (DWSIM.UnitOperations.UnitOperations.Cooler)c1;

            cooler.CalcMode = DWSIM.UnitOperations.UnitOperations.Cooler.CalculationMode.OutletTemperature;
            cooler.OutletTemperature = 320; // K

            // set flowsheet solver messages listener

            sim.SetMessageListener((msg, mtype) => Console.WriteLine(msg));

            // request a calculation

            interf.CalculateFlowsheet(sim, null);

            // save file as dwxmz (compressed XML)

            string fileNameToSave = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "created_file.dwxmz");

            interf.SaveFlowsheet2(sim, fileNameToSave);

            Console.WriteLine(String.Format("Cooler Heat Load: {0} kW", cooler.DeltaQ.GetValueOrDefault()));

            Console.WriteLine("Finished OK!");

        }

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(DWSIMLibrariesDir, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
            {
                string assemblyPath2 = Path.Combine(DWSIMLibrariesDir, new AssemblyName(args.Name).Name + ".exe");
                if (!File.Exists(assemblyPath2))
                {
                    return null;
                }
                else
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyPath2);
                    return assembly;
                }
            }
            else
            {
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            }
        }
    }
}