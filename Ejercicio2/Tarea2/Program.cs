using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Tarea2
{
    public enum Estado
    {
        EspConsulta,
        Consulta,
        EspDiagnostico,
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
            Thread T1 = new (Consulta);
            Thread T2 = new (Consulta);
            Thread T3 = new (Consulta);
            Thread T4 = new (Consulta);

            Console.WriteLine("ID PACIENTE\tORDEN LLEGADA\tPRIORIDAD\tESTADO ACTUAL\t\tTRANSICION ESTADO\tTIEMPO TOTAL");
            T1.Start();
            Thread.Sleep(2000);
            T2.Start();
            Thread.Sleep(2000);
            T3.Start();
            Thread.Sleep(2000);
            T4.Start();

            T1.Join();
            T2.Join();
            T3.Join();
            T4.Join();

            // Ahora que tenemos la lista de pacientes cargada, vamos a pasar por diagnosis a aquellos que lo requieran
            foreach (Paciente paciente in IntercambioPacientes)
            {

                Thread.Sleep(500); // Gracias a este sleep podemos controlar que los pacientes vayan llegando a diagnosis en orden de llegada

                Task.Run(()=>{
                    int CronoMain;
                    if (paciente.RequiereDiagnostico) 
                    {
                        // Mientras el paciente llega le cambiamos los datos del usuario, los visualizamos y reiniciamos le cronómetro
                        paciente.Estado = Estado.EspDiagnostico;
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
                });
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
            Console.WriteLine("Paciente {0}. \tLlegado el {1}. \tPrioridad: N{2} \tEstado: {3} \tDuración: {4} segundos.\tTiempo Total: {5} segundos",
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