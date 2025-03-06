using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Tarea2
{
    class Program
    {   
        static SemaphoreSlim SemMedicos = new SemaphoreSlim(4);
        static ConcurrentQueue<Paciente> ColaPacientes = new ConcurrentQueue<Paciente>();
        static BlockingCollection<Paciente> Intercambio = new BlockingCollection<Paciente>(ColaPacientes);
        static Stopwatch stopwatch = Stopwatch.StartNew();
        static int Llegadas = 1;

        static void Main(string[] args)
        {
            Thread T1 = new Thread(Consulta);
            Thread T2 = new Thread(Consulta);
            Thread T3 = new Thread(Consulta);
            Thread T4 = new Thread(Consulta);

            T1.Start();
            Thread.Sleep(2000);
            T2.Start();
            Thread.Sleep(2000);
            T3.Start();
            Thread.Sleep(2000);
            T4.Start();

            Console.ReadLine();
        }

        static void Consulta ()
        {
            SemMedicos.Wait();
            Console.WriteLine("Ha entrado el {0} paciente", Llegadas);
            int tiempoLlegada = (int)stopwatch.Elapsed.TotalSeconds;
            Paciente paciente = new Paciente(IdAleatorio(), tiempoLlegada, numAleatorio(), Llegadas++, 1);
            Intercambio.Add(paciente);
            Thread.Sleep(paciente.TiempoConsulta*1000);
            SemMedicos.Release();
            Console.WriteLine("Ha salido el paciente {0}: tiempo llegada: {1}s, tiempo consulta: {2}s, orden de llegada: {3}, prioridad: N{4}",
             paciente.Id, paciente.LlegadaHospital, paciente.TiempoConsulta, paciente.NumeroLlegada, paciente.Prioridad);
        }

        private static int numAleatorio()
        {
            Random rnd = new Random();
            return rnd.Next(5,16);
        }

        private static int IdAleatorio()
        {
            Random rnd = new Random();
            bool disponible = false;
            int id = 0;
            while(!disponible)
            {
                disponible = true;
                id = rnd.Next(1, 101);
                Parallel.ForEach (Intercambio, paciente => {
                    if (paciente.Id == id) disponible = false;
                });
            }
            return id;
        }
    
    }

    public class Paciente
    {
        public int Id {get;set;}
        public int LlegadaHospital {get;set;}
        public int TiempoConsulta {get;set;}
        public int Estado {get;set;}
        public int NumeroLlegada {get;set;}
        public int Prioridad {get;set;}

        public Paciente (int Id, int LlegadaHospital, int TiempoConsulta, int NumeroLlegada, int Prioridad)
        {
            this.Id = Id;
            this.LlegadaHospital = LlegadaHospital;
            this.TiempoConsulta = TiempoConsulta;
            this.Estado = 0;
            this.NumeroLlegada = NumeroLlegada;
            this.Prioridad = Prioridad;
        }
    }
}