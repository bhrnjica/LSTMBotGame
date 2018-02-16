using CNTK;
using System;
using System.Threading.Tasks;

namespace MarioKart.Bot.NET
{

    internal class Program
    {
        static async Task  Main(string[] args)
        {
            Console.Title = ".NET Marion Kart Bot";
            Console.WriteLine("Waiting for 5 sec in order to prepare the game window!");
            await Task.Delay(5000);

            ///*** generate training data ***
            /// simple play Mario game while the application is running
            /// the app will automatically collect training data
            /// N64 emulator must be in top left corner with 800x640 resolution
            /// In case N64 emulator is not running no data is generating

            //uncomment this line to generate training data
            await GenerateData.Start();


            //***teach Mario to drive Kart by train LSTM RNN model***

            //uncomment this line to train Mario usually do that on GPU
            //run this line in case you have GPU compatible graphic card
            //CNTKDeepNN.Train(DeviceDescriptor.GPUDevice(0));
            //run this in case you dont have GPU
            //CNTKDeepNN.Train(DeviceDescriptor.CPUDevice);


            //*****play a game****
            //uncomment those three lines to play game with RNN model
            //var dev = DeviceDescriptor.CPUDevice;
            //MarioKartPlay.LoadModel("../../../../training/mario_kart_modelv1", dev);
            //MarioKartPlay.PlayGame(dev);

        }
    }
}