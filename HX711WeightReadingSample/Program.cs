using System;
using System.Threading;
using Alonf.Netduino.Sensors;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace HX711WeightReadingSample
{
    public class Program
    {
        static readonly OutputPort Led = new OutputPort(Pins.ONBOARD_LED, false);
        public static void Main()
        {
            // parameter "gain" is ommited; the default value 128 is used by the library
            var scale = new HX711(Pins.GPIO_PIN_D1, Pins.GPIO_PIN_D0)
                        {
                            Scale = 2280.0
                        };
            

            // this value is obtained by calibrating the scale with known weights; see the https://github.com/bogde/HX711 for details
            scale.Tare();

            while (true)
            {
                Loop(scale);
            }
        }

        private static void Loop(HX711 scale)
        {
            var result = scale.GetUnits(10);

            //Blink the on board led according to the weight
            for (int i = 0; i < Math.Abs(result/10); i++)
            {
                Led.Write(true);
                Thread.Sleep(100);
                Led.Write(false);
                Thread.Sleep(100);
            }

            scale.PowerDown(); // put the ADC in sleep mode for two seconds
            Thread.Sleep(2000);
            scale.PowerUp();
        }
    }
}
