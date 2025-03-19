using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Tarea1
{
    public enum Estado
    {
        EspConsulta,
        Consulta,
        EspDiagnosis,
        Finalizado
    }

    class Program
    {
        static SemaphoreSlim SemAtencion = new (0);
        static SemaphoreSlim SemMedicos = new (4);
        static SemaphoreSlim SemDiagnosis = new (2);
        static BlockingCollection<int> IntercambioIds = new BlockingCollection<int>();
        static BlockingCollection<Paciente> PacientesN1 = new BlockingCollection<Paciente>();
        static BlockingCollection<Paciente> PacientesN2 = new BlockingCollection<Paciente>();
        static BlockingCollection<Paciente> PacientesN3 = new BlockingCollection<Paciente>();
        static readonly Stopwatch MainStopwatch = Stopwatch.StartNew();
        static readonly object locker = new object();
        static bool FinDePrograma = false;
        static int Llegadas = 1;
        const int PacientesTotales = 500;
        static int[] PacientesAtendidos = [0, 0, 0];
        static int[] TiemposEspera = [0, 0, 0];
        static int PacientesDiagnosticados = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("ID PACIENTE.\tORDEN LLEGADA.\tPRIORIDAD.\tESTADO ACTUAL.\tTRANSICION ESTADO.\tTIEMPO TOTAL");

            Thread TGestionLlegadas = new(GestionarLlegadas);
            TGestionLlegadas.Start();

            List<Thread> hilosAtencion = new();
            Task TareaAtencion = Task.Run(()=>{
                while(true)
                {
                    if (PacientesN1.Count > 0 || PacientesN2.Count > 0 || PacientesN3.Count > 0)
                    {
                        SemMedicos.Wait();
                        Paciente paciente = ExtraerPacientePrioritario();
                        Thread TAtenderPaciente = new (AtenderPaciente!);
                        hilosAtencion.Add(TAtenderPaciente);
                        TAtenderPaciente.Start(paciente);
                    } else if (FinDePrograma) {
                        break;
                    }
                    if (!FinDePrograma) SemAtencion.Wait();
                }
            });

            TareaAtencion.Wait();
            foreach (var hilo in hilosAtencion)
            {
                hilo.Join();
            }

            Console.WriteLine("Todos los pacientes han sido atendidos. Presiona Enter para visualizar las estadísticas.");
            Console.ReadLine();
            MostrarStats();
        }

        private static void GestionarLlegadas ()
        {
            for (int i = 0; i < PacientesTotales; i++)
            {
                int TiempoLlegada = (int) MainStopwatch.Elapsed.TotalSeconds;
                Paciente paciente = new(IdAleatorio(), TiempoLlegada, NumAleatorio(5,15), Llegadas++, NumAleatorio(1,3));
                MostrarInformacion(paciente);
                switch (paciente.Prioridad)
                {
                    case 1:
                        PacientesN1.Add(paciente);
                        lock(locker)
                        {
                            PacientesAtendidos[0]++;
                        }
                        break;
                    
                    case 2:
                        PacientesN2.Add(paciente);
                        lock(locker)
                        {
                            PacientesAtendidos[1]++;
                        }
                        break;
                    
                    case 3:
                        PacientesN3.Add(paciente);
                        lock(locker)
                        {
                            PacientesAtendidos[2]++;
                        }
                        break;
                }
                SemAtencion.Release();
                Thread.Sleep(2000);
            }

            lock(locker)
            {
                FinDePrograma = true;
            }
            SemAtencion.Release();

        }

        private static void AtenderPaciente (Object PacienteObjeto)
        {
            Paciente paciente = (Paciente)PacienteObjeto;
            OperarEstado(Estado.Consulta, paciente);

            Thread.Sleep(paciente.TiempoConsulta*1000);
            paciente.RequiereDiagnostico = NumAleatorio(0,1) != 0;

            if(paciente.RequiereDiagnostico)
            {
                OperarEstado(Estado.EspDiagnosis, paciente);
                SemMedicos.Release();
                SemDiagnosis.Wait();
                Thread.Sleep(15000);
                SemDiagnosis.Release();
                lock(locker) PacientesDiagnosticados++;
            }

            OperarEstado(Estado.Finalizado, paciente);
            if(!paciente.RequiereDiagnostico) SemMedicos.Release();
        }

        private static Paciente ExtraerPacientePrioritario ()
        {
            while(true)
            {
                if (PacientesN1.Count > 0) return PacientesN1.Take();
                else if (PacientesN2.Count > 0) return PacientesN2.Take();
                else if (PacientesN3.Count > 0) return PacientesN3.Take();
            }
        }

        private static void OperarEstado (Estado estado, Paciente paciente)
        {
            paciente.Estado = estado;
            int CronoMain = (int) MainStopwatch.Elapsed.TotalSeconds;
            paciente.TiempoEstados = CronoMain - paciente.TiempoTotal;
            paciente.TiempoTotal = CronoMain;
            MostrarInformacion(paciente);
            if (estado == Estado.Consulta)
            {
                switch (paciente.Prioridad)
                {
                    case 1:
                        lock(locker)
                        {
                            TiemposEspera[0] += paciente.TiempoEstados;
                        }
                        break;
                    
                    case 2:
                        lock(locker)
                        {
                            TiemposEspera[1] += paciente.TiempoEstados;
                        }
                        break;
                    
                    case 3:
                        lock(locker)
                        {
                            TiemposEspera[2] += paciente.TiempoEstados;
                        }
                        break;
                }
            }
        }

        private static int NumAleatorio(int min, int max)
        {
            Random rnd = new Random();
            return rnd.Next(min,max+1);
        }

        private static int IdAleatorio()
        {
            bool disponible = false;
            int id = 0;
            while(!disponible)
            {
                disponible = true;
                id = NumAleatorio(1,10000);
                Parallel.ForEach (IntercambioIds, Identificador => {
                    if (Identificador == id) disponible = false;
                });
            }
            IntercambioIds.Add(id);
            return id;
        }
    
        private static void MostrarInformacion (Paciente paciente)
        {
            Console.WriteLine("Paciente {0}. \tLlegada {1}. \tPrioridad: N{2}. \t{3}. \tDuración: {4} segundos.\tTiempo Total: {5} segundos",
                paciente.Id, paciente.NumeroLlegada, paciente.Prioridad, paciente.Estado, paciente.TiempoEstados, paciente.TiempoTotal);
        }

        private static void MostrarStats()
        {
            Console.WriteLine("--- FIN DEL DÍA ---");

            Console.WriteLine("\nPacientes atendidos: ");
            Console.WriteLine("\t- Emergencias: {0}\n\t- Urgencias: {1}\n\t- Consultas generales: {2}",
                PacientesAtendidos[0], PacientesAtendidos[1], PacientesAtendidos[2]);
            
            Console.WriteLine("\nTiempo promedio de espera: ");
            Console.WriteLine("\t- Emergencias: {0}s\n\t- Urgencias: {1}s\n\t- Consultas generales: {2}s",
                TiemposEspera[0] / PacientesAtendidos[0], 
                TiemposEspera[1] / PacientesAtendidos[1], 
                TiemposEspera[2] / PacientesAtendidos[2]);

            Console.WriteLine("\nUso promedio de máquinas de diagnóstico: {0}%", ((PacientesDiagnosticados * 100) / PacientesTotales));

            Console.WriteLine("\n\nPresiona ENTER para salir");
            Console.ReadLine();
        }
    }

    public class Paciente
    {
        public int Id {get;set;}
        public int TiempoEstados {get;set;}
        public int TiempoConsulta {get;set;}
        public Estado Estado {get;set;}
        public int NumeroLlegada {get;set;}
        public int Prioridad {get;set;}
        public int TiempoTotal {get;set;}
        public bool RequiereDiagnostico {get;set;}
        public int MarcaTiempo {get;set;}

        public Paciente (int Id, int TiempoEstados, int TiempoConsulta, int NumeroLlegada, int Prioridad)
        {
            this.Id = Id;
            this.TiempoEstados = TiempoEstados;
            this.TiempoConsulta = TiempoConsulta;
            this.Estado = Estado.EspConsulta;
            this.NumeroLlegada = NumeroLlegada;
            this.Prioridad = Prioridad;
            this.TiempoTotal = TiempoEstados;
            this.RequiereDiagnostico = false;
            this.MarcaTiempo = TiempoEstados;
        }
    }
}