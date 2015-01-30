using Microsoft.SPOT.Hardware;

// A port from bogde Arduino library: https://github.com/bogde/HX711 
// For this class in the Netduino Alonf library follow the original license: https://github.com/bogde/HX711/blob/master/LICENSE
// Be aware that using a debugger may cause wrong results

namespace Alonf.Netduino.Sensors
{
    public class HX711
    {
        private static OutputPort _pdSck; // Power Down and Serial Clock Input Pin
        private static InputPort _dOut; // Serial Data Output Pin
        private byte _gain; // amplification factor

        // define clock and data pin, channel, and gain factor
        // channel selection is made by passing the appropriate gain: 128 or 64 for channel A, 32 for channel B
        // gain: 128 or 64 for channel A; channel B works with 32 gain factor only
        public HX711(Cpu.Pin dout, Cpu.Pin pdSck, byte gain = 128)
        {
            _pdSck = new OutputPort(pdSck, false);
            _dOut = new InputPort(dout, false, Port.ResistorMode.Disabled);
            SetGain(gain);
            Offset = 0;
            Scale = 1;
        }

        // check if HX711 is ready
        // from the data sheet: When output data is not ready for retrieval, digital output pin DOUT is high. Serial clock
        // input PD_SCK should be low. When DOUT goes to low, it indicates data is ready for retrieval.
        public bool Ready
        {
            get
            {
                _pdSck.Write(false);
                var bit = _dOut.Read();
                return !bit;
            }
        }

        // set the gain factor; takes effect only after a call to read()
        // channel A can be set for a 128 or 64 gain; channel B has a fixed 32 gain
        // depending on the parameter, the channel is also set to either A or B
        private void SetGain(byte gain)
        {
            switch (gain)
            {
                case 128: // channel A, __gain factor 128
                    _gain = 1;
                    break;
                case 64: // channel A, _gain factor 64
                    _gain = 3;
                    break;
                case 32: // channel B, _gain factor 32
                    _gain = 2;
                    break;
            }
            _pdSck.Write(false);
            Read();
        }

        // waits for the chip to be ready and returns a reading
        public int Read()
        {
            // wait for the chip to become ready
            while (!Ready)
            {
            }

            var data = new byte[3];

            // pulse the clock pin 24 times to read the data
            for (byte j = 3; j-- != 0; )
            {
                data[j] = 0;
                for (byte i = 8; i-- != 0; )
                {
                    _pdSck.Write(true);
                    var bit = _dOut.Read();
                    byte bitValue = bit ? (byte)1 : (byte)0;
                    data[j] |= (byte)(bitValue << i);
                    _pdSck.Write(false);
                }
            }

            // set the channel and the gain factor for the next reading using the clock pin
            for (int i = 0; i < _gain; ++i)
            {
                _pdSck.Write(true);
                _pdSck.Write(false);
            }
            data[2] ^= 0x80;
            int result = (data[2] << 16) | (data[1] << 8) | data[0];
            return result;
        }

        // returns an average reading; times = how many times to read
        public int ReadAverage(byte times = 10)
        {
            int sum = 0;
            for (byte i = 0; i < times; ++i)
                sum += Read();

            return sum / times;
        }


        // returns (read_average() - OFFSET), that is the current value without the tare weight; times = how many readings to do
        public double GetValue(byte times = 1)
        {
            return ReadAverage(times) - Offset;
        }

        // returns get_value() divided by SCALE, that is the raw value divided by a value obtained via calibration
        // times = how many readings to do
        public double GetUnits(byte times = 1)
        {
            return GetValue(times) / Scale;
        }

        public long Offset { get; set; } // used for tare weight
        public double Scale { get; set; } // used to return weight in grams, kg, ounces, whatever

        // set the OFFSET value for tare weight; times = how many times to read the tare value
        public void Tare(byte times = 10)
        {
            int sum = ReadAverage(times);
            Offset = sum;
        }

        // puts the chip into power down mode
        public void PowerDown()
        {
            _pdSck.Write(false);
            _pdSck.Write(true);
        }

        // wakes up the chip after power down mode
        public void PowerUp()
        {
            _pdSck.Write(false);
        }
    }
}