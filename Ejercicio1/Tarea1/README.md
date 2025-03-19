# Ejercicio 1 – Consulta Médica – Tarea 1

## **Propósito del Proyecto**
Este proyecto simula la llegada de 4 pacientes a un hospital y su atención médica en consultas disponibles. Utilizando un semáforo con 4 permisos, cada médico atiende a un paciente de forma individual. Los pacientes llegan con un intervalo de 2 segundos entre ellos, y cada consulta médica tiene una duración de 10 segundos.

El código demuestra cómo gestionar **procesos concurrentes** mediante la clase `SemaphoreSlim` en C#, garantizando que no haya más de 4 pacientes atendidos simultáneamente y manejando la sincronización de recursos.

---

## **Especificaciones del Código**
1. **Llegada de Pacientes:**
   - Se utiliza un hilo (`Thread`) para cada uno de los 4 pacientes.
   - Los hilos simulan pacientes que llegan al hospital con un intervalo de 2 segundos.

2. **Atención Médica:**
   - Se utiliza un objeto `SemaphoreSlim` inicializado con un valor de 4 para representar los 4 médicos disponibles.
   - Si un médico está ocupado, los pacientes esperan su turno.

3. **Duración y Mensajes:**
   - Los pacientes se atienden durante 10 segundos (`Thread.Sleep(10000)`).
   - Se muestran mensajes en la consola indicando la llegada y salida de cada paciente.

---

## **Métodos y clases que implementa este proyecto**
1. **Clase `LlegadaPaciente`**
   - Cuando un hilo inicia este método hace un `Wait` sobre el semáforo de médicos (4).
   - Una vez el semáforo le asigne su turno, se genera un número de paciente en funcion de su llegada.
   - Se notifica por consola que el paciente ha llegado a la consulta.
   - Se utiliza un `Thread.Sleep(10000)` para pausar el hilo 10 segundos.
   - Se ejecuta un `Release` sobre el semáforo.
   - Se notifica por consola que el paciente ha salido la consulta.

---

## **Preguntas y Respuestas**

1. **¿Cuántos hilos se están ejecutando en este programa? Explica tu respuesta.**
   Se ejecutan **5 hilos**:
   - El hilo principal, que coordina y lanza los hilos de los pacientes.
   - 4 hilos adicionales, uno por cada paciente, que ejecutan el método `LlegadaPaciente`.

2. **¿Cuál de los pacientes entra primero en consulta? Explica tu respuesta.**
   El **Paciente 1** es el primero en entrar a consulta, ya que el hilo principal "duerme" 2 segundos entre la inicialización de cada paciente. Esto asegura que los hilos se ejecuten secuencialmente y que el Paciente 1 siempre tenga acceso inmediato a un médico disponible.

3. **¿Cuál de los pacientes sale primero de consulta? Explica tu respuesta.**
   El **Paciente 1** es el primero en salir de consulta, debido a los tiempos secuenciales:
   - Paciente 1: 10 segundos de atención.
   - Paciente 2: 2 segundos para llegar más 10 segundos de atención.
   - Paciente 3: 4 segundos para llegar más 10 segundos de atención.
   - Paciente 4: 6 segundos para llegar más 10 segundos de atención.

---

## **Capturas de Pantalla**
### **Ejecución del Programa**
![](e1-t1-output.png)

---

## **Diagrama de flujo**
![](e1-t1-esquema.png)

---
