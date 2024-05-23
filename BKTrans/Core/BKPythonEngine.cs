using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Python.Runtime;

namespace BKAssembly;

public class BKPythonEngine : IDisposable
{
    static Lazy<BKPythonEngine> _instance;
    nint _threadState { get; set; }

    private BKPythonEngine(string pythonDll)
    {
        // https://github.com/pythonnet/pythonnet/wiki/Threading
        Runtime.PythonDLL = pythonDll;
        PythonEngine.Initialize();
        _threadState = PythonEngine.BeginAllowThreads();
    }

    public void Dispose()
    {
        //PythonEngine.EndAllowThreads(_threadState);
        //PythonEngine.Shutdown();
    }

    public static void Initialize(string pythonDll = "python311.dll")
    {
        if (_instance.IsNull())
        {
            _instance = new(new BKPythonEngine(pythonDll));
            _ = _instance.Value;
        }
    }

    public static void Shutdown()
    {
        if (_instance.IsNotNull())
            _instance.Value.Dispose();
    }
}
