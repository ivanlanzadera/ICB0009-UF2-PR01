using System;

namespace _Tarea1
{
    class Program
    {

        static int pacientes = 0;
        
        // Generamos un semáforo de 4 elementos (4 doctores)
        static SemaphoreSlim SemMedicos = new SemaphoreSlim(4);

        static void Main(string[] args)
        {
            Thread Paciente1 = new Thread(LlegadaPaciente);
            Thread Paciente2 = new Thread(LlegadaPaciente);
            Thread Paciente3 = new Thread(LlegadaPaciente);
            Thread Paciente4 = new Thread(LlegadaPaciente);

            Paciente1.Start();
            Thread.Sleep(2000);
            Paciente2.Start();
            Thread.Sleep(2000);
            Paciente3.Start();
            Thread.Sleep(2000);
            Paciente4.Start();

            Console.ReadLine();
        }

        static void LlegadaPaciente ()
        {
            SemMedicos.Wait();
            string paciente = "Paciente "+(++pacientes);
            Console.WriteLine("Ha llegado el {0}", paciente);
            Thread.Sleep(10000);
            SemMedicos.Release();
            Console.WriteLine("Ha salido el {0}", paciente);
        }
    }
}