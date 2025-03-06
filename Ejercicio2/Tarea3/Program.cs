using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Tarea3
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
        static SemaphoreSlim SemMedicos = new SemaphoreSlim(4);
        static SemaphoreSlim SemDiagnosis = new SemaphoreSlim(2);
        static ConcurrentQueue<Paciente> ColaPacientes = new ConcurrentQueue<Paciente>();
        static BlockingCollection<Paciente> IntercambioPacientes = new BlockingCollection<Paciente>(ColaPacientes);
        static ConcurrentQueue<int> ColaIds = new ConcurrentQueue<int>();
        static BlockingCollection<int> IntercambioIds = new BlockingCollection<int>();
        static readonly Stopwatch MainStopwatch = Stopwatch.StartNew();
        static int Llegadas = 1;

        static void Main(string[] args)
        {
            Console.WriteLine("ID PACIENTE.\tORDEN LLEGADA.\tPRIORIDAD.\tESTADO ACTUAL.\tTRANSICION ESTADO.\tTIEMPO TOTAL");

            for (int i = 0; i<20; i++)
            {
                Thread TPaciente = new (Consulta);
                TPaciente.Start();
                Thread.Sleep(2000);
            }

            Console.ReadLine();
        }

        static void Consulta ()
        {
            int CronoMain;
            // LLega el paciente pero no entra a consulta
            int TiempoLlegada = (int) MainStopwatch.Elapsed.TotalSeconds;
            Paciente paciente = new(IdAleatorio(), TiempoLlegada, NumAleatorio(5,15), Llegadas++, NumAleatorio(1,3));
            IntercambioPacientes.Add(paciente);
            MostrarInformacion(paciente);

            SemMedicos.Wait(); // El paciente entra en la consulta

            // Modificamos el estado del paciente, establecemos la duración del estado de espera y reiniciamos el crono del hilo
            paciente.Estado = Estado.Consulta;
            CronoMain = (int) MainStopwatch.Elapsed.TotalSeconds;
            paciente.TiempoEstados = CronoMain - paciente.MarcaTiempo;
            paciente.MarcaTiempo = CronoMain;
            paciente.TiempoTotal += paciente.TiempoEstados;
            MostrarInformacion(paciente);
            Thread.Sleep(paciente.TiempoConsulta*1000);

            paciente.RequiereDiagnostico = NumAleatorio(0,1) != 0;
            SemMedicos.Release(); // El paciente ha terminado la consulta médica y abandona la sala

            if (paciente.RequiereDiagnostico)
            {
                // Mientras el paciente llega le cambiamos los datos del usuario, los visualizamos.
                paciente.Estado = Estado.EspDiagnosis;
                CronoMain = (int) MainStopwatch.Elapsed.TotalSeconds;
                paciente.TiempoEstados = CronoMain - paciente.MarcaTiempo;
                paciente.TiempoTotal += paciente.TiempoEstados;
                MostrarInformacion(paciente);
                
                SemDiagnosis.Wait();
                Thread.Sleep(15000);

                // Cuando el paciente termina de usar la máquina la liberamos
                SemDiagnosis.Release();   
            }
            // Modificamos su estado y establecemos la duración del cambio de estado
            paciente.Estado = Estado.Finalizado;
            CronoMain = (int) MainStopwatch.Elapsed.TotalSeconds;
            paciente.TiempoEstados = CronoMain - paciente.MarcaTiempo;
            paciente.TiempoTotal += paciente.TiempoEstados;
            MostrarInformacion(paciente);
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
                id = NumAleatorio(1,100);
                Parallel.ForEach (IntercambioIds, Identificador => {
                    if (Identificador == id) disponible = false;
                });
            }
            IntercambioIds.Add(id);
            return id;
        }
    
        private static void MostrarInformacion (Paciente paciente)
        {
            Console.WriteLine("Paciente {0}. \tLlegado el {1}. \tPrioridad: N{2}. \t{3}. \tDuración: {4} segundos.\tTiempo Total: {5} segundos",
                paciente.Id, paciente.NumeroLlegada, paciente.Prioridad, paciente.Estado, paciente.TiempoEstados, paciente.TiempoTotal);
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