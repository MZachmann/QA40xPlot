using static QA40xPlot.QA430.QA430Model;

namespace QA40xPlot.QA430
{
    internal static class BitOps
    {
		internal static UInt32 BitSet(this UInt32 val, UInt32 bitMask)
		{
			UInt32 r = val;

			r |= bitMask;

			return r;
		}

		internal static UInt32 BitClear(this UInt32 val, UInt32 bitMask)
		{
			UInt32 r = val;

			r &= ~bitMask;

			return r;
		}
	}

    //the higher-level control portion
	internal class Hw
    {
		internal static UInt32 SetDefaults()
        {
            UInt32 writeVal = 0;
            SetPositiveRailVoltage(1.0);
            SetNegativeRailVoltage(1.0);
            ResetAllRelays();

            writeVal = SetPowerFromAdjustableRails(writeVal);

            //Set gain of +1
            writeVal = SetOpampConfig6a(writeVal);

            // don't allow analyzer to drive rails
            writeVal = SetPsrr(PsrrOptions.BothPsrrInputsGrounded, writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);

            return writeVal;
        }

        internal static void ResetAllRelays()
        {
            Qa430Usb.Singleton.WriteRegister(5, 0);
        }

        internal static async Task WaitForRelays()
        {
            await Task.Delay(500);
		}

        internal static UInt32 ResetOpampPathRelays(UInt32 writeVal)
        {
            // Need to clear relays 0..8. That's 9 bits.
            // We don't want to touch relay 9 and above, as 
            // these control load, supplies, current sense, 
            // PSRR.
            writeVal = writeVal.BitClear(0x1FF);
            return writeVal;
        }


        /// <summary>
        /// 60 dB gain for BW measurement
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig1(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);

            // Select 4.99 ohm series
            writeVal = SetNegInput(OpampNegInputs.GndTo4p99, writeVal);

            // Select 4.99K feedback
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);

            // Select no connection between inverting and non-inverting
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);

            // Selection opamp + input to analyzer
            writeVal = SetPosInput(OpampPosInputs.Analyzer, writeVal);

            return writeVal;
        }

        /// <summary>
        /// 60 dB gain for CMRR measurement
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig2(UInt32 writeVal)
        {
            int targetGain = 20;


            writeVal = ResetOpampPathRelays(writeVal);
            if (targetGain == 0)
            {
                writeVal = SetFeedback(OpampFeedbacks.Short, writeVal);
                writeVal = SetNegInput(OpampNegInputs.Open, writeVal); 
            }
            else if (targetGain == 20)
            {
                writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
                writeVal = SetNegInput(OpampNegInputs.GndTo499, writeVal); 
            }
            else if (targetGain == 60)
            {
                writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
                writeVal = SetNegInput(OpampNegInputs.GndTo4p99, writeVal); 
            }
            else
            {
                // Bad gain
                throw new Exception("Bad gain in SEtOpampConfig2()");
            }
            //writeVal = SetNegInput(OpampNegInputs.GndTo4p99, writeVal); // 60 dB of gain
            //writeVal = SetNegInput(OpampNegInputs.GndTo499, writeVal);  // 20 dB of gain
            //writeVal = SetFeedback(OpampFeedback.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Short, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Analyzer, writeVal);
            return writeVal;
        }

        /// <summary>
        ///  60 dB Gain for Offset and Noise Measurement
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig3a(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.GndTo4p99, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Gnd, writeVal);
            return writeVal;
        }

        /// <summary>
        /// 20 dB Gain for Offset and Noise Measurement
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig3b(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.GndTo499, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Gnd, writeVal);
            return writeVal;
        }

        /// <summary>
        /// 0 dB Gain for Offset and Noise Measurement
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig3c(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.Open, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.Short, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Gnd, writeVal);
            return writeVal;
        }

        /// <summary>
        /// SigGain = 10, Dist Gain = 10
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig4a(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.GndTo499, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Analyzer, writeVal);
            return writeVal;
        }

        /// <summary>
        /// SigGain = 10, Dist Gain = 110
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig4b(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.GndTo499, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.R49p9, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Analyzer, writeVal);
            return writeVal;
        }

        /// <summary>
        /// Sig Gain = -10, Dist Gain = 100
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig5a(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.AnalyzerTo499, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Gnd, writeVal);
            return writeVal;
        }

        /// <summary>
        /// Sig Gain = -10, Dist Gain = 110
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig5b(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.AnalyzerTo499, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.R49p9, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Gnd, writeVal);
            return writeVal;
        }

        /// <summary>
        /// Sig gain = 1, distortion gain = 1
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig6a(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.Open, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Analyzer, writeVal);
            return writeVal;
        }

        /// <summary>
        /// Sig gain = 1, distortion gain = 101
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig6b(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.Open, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.R49p9, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Analyzer, writeVal);
            return writeVal;
        }

        /// <summary>
        /// Sig gain = 1, distortion gain = 1
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig7a(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.AnalyzerTo4p99k, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Gnd, writeVal);
            return writeVal;
        }

        /// <summary>
        /// Sig gain = 1, distortion gain = 101
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig7b(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.AnalyzerTo4p99k, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.R4p99k, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.R49p9, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Gnd, writeVal);
            return writeVal;
        }

        /// <summary>
        /// Input Linearity and Noise
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig8a(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.Open, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.Short, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.Analyzer, writeVal);
            return writeVal;
        }

        /// <summary>
        /// Input Linearity and Noise
        /// </summary>
        /// <param name="writeVal"></param>
        /// <returns></returns>
        internal static UInt32 SetOpampConfig8b(UInt32 writeVal)
        {
            writeVal = ResetOpampPathRelays(writeVal);
            writeVal = SetNegInput(OpampNegInputs.Open, writeVal);
            writeVal = SetFeedback(OpampFeedbacks.Short, writeVal);
            writeVal = SetPosNegConnect(OpampPosNegConnects.Open, writeVal);
            writeVal = SetPosInput(OpampPosInputs.AnalyzerTo100k, writeVal);
            return writeVal;
        }

        //internal static UInt32 SetPSRRInputToGround(UInt32 writeVal)
        //{
        //    writeVal.BitSet(0x1 << 14);
        //    writeVal.BitSet(0x1 << 15);
        //    return writeVal;
        //}

        //internal static UInt32 SetPSRRInputToAnalyzer(UInt32 writeVal)
        //{
        //    writeVal.BitClear(0x1 << 14);
        //    writeVal.BitClear(0x1 << 15);
        //    return writeVal;
        //}

        internal static UInt32 SetPsrr(PsrrOptions options)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetPsrr(options, writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
            return writeVal;
		}

		internal static UInt32 SetPsrr(PsrrOptions options, UInt32 writeVal)
        {
            switch (options)
            {
                case PsrrOptions.BothPsrrInputsGrounded:
                    writeVal = writeVal.BitSet(0x1 << 14);
                    writeVal = writeVal.BitSet(0x1 << 15);
                    break;
                case PsrrOptions.HiRailToAnalyzer:
                    writeVal = writeVal.BitClear(0x1 << 14);
                    writeVal = writeVal.BitSet(0x1 << 15);
                    break;
                case PsrrOptions.LowRailToAnalyzer:
                    writeVal = writeVal.BitClear(0x1 << 15);
                    writeVal = writeVal.BitSet(0x1 << 14);
                    break;
                case PsrrOptions.BothRailsToAnalyzer:
                    writeVal = writeVal.BitClear(0x1 << 14);
                    writeVal = writeVal.BitClear(0x1 << 15);
                    break;
                default:
                    break;
            }

            return writeVal;
        }

        internal static void SetPsrrNow(PsrrOptions options)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetPsrr(options, writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
        }


        //internal static void SetPSRRInputToGroundNow()
        //{
        //    UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
        //    writeVal.BitSet(0x1 << 14);
        //    writeVal.BitSet(0x1 << 15);

        //    Qa430Usb.Singleton.WriteRegister(5, writeVal);
        //}

        //internal static void SetPSRRInputToAnalyzerNow()
        //{
        //    UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
        //    writeVal.BitClear(0x1 << 14);
        //    writeVal.BitClear(0x1 << 15);

        //    Qa430Usb.Singleton.WriteRegister(5, writeVal);
        //}

        internal static UInt32 OpampSupplyEnable(bool isEnabled)
        {
            UInt32 val = Qa430Usb.Singleton.ReadRegister(0xA);
            if (isEnabled)
                val = val.BitSet(0x1);
            else
                val = val.BitClear(0x1);
			Qa430Usb.Singleton.WriteRegister(0xA, val);
			return val;
		}

		internal static void OpampSupplyEnableNow()
        {

            UInt32 val = Qa430Usb.Singleton.ReadRegister(0xA);
            Qa430Usb.Singleton.WriteRegister(0xA, val | 0x1);
        }

        internal static void OpampSupplyDisableNow()
        {
            UInt32 val = Qa430Usb.Singleton.ReadRegister(0xA);
            Qa430Usb.Singleton.WriteRegister(0xA, (uint)(val & ~0x1));
        }

        internal static UInt32 SetPowerRails(bool isFixed)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            if (isFixed)
                writeVal = SetPowerFromFixedRails(writeVal);
            else
                writeVal = SetPowerFromAdjustableRails(writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
            return writeVal;
		}

        internal static UInt32 SetPowerFromFixedRails(UInt32 writeVal)
        {
            writeVal = writeVal.BitSet(0x1 << 13);
            return writeVal;
        }

        internal static UInt32 SetPowerFromAdjustableRails(UInt32 writeVal)
        {
            writeVal = writeVal.BitClear(0x1 << 13);
            return writeVal;
        }

        internal static void SetPowerFromFixedRailsNow()
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetPowerFromFixedRails(writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
        }

        internal static void SetPowerFromAdjustableRailsNow()
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetPowerFromAdjustableRails(writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
        }

        internal static UInt32 EnableSplitCurrentSense(bool isEnable)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            if (isEnable)
                writeVal = writeVal.BitSet(0x1 << 12);
            else
				writeVal = writeVal.BitClear(0x1 << 12);
			Qa430Usb.Singleton.WriteRegister(5, writeVal);
            return writeVal;
		}

		internal static UInt32 EnableSplitCurrentSense(UInt32 writeVal)
        {
            writeVal = writeVal.BitClear(0x1 << 12);
            return writeVal;
        }

        internal static UInt32 DisableSplitCurrentSense(UInt32 writeVal)
        {
            writeVal = writeVal.BitSet(0x1 << 12);
            return writeVal;
        }

        internal static void SetPositiveRailVoltage(double volts)
        {
            byte counts = ComputeCountsFromVoltage(volts);
            //Debug.WriteLine($"Positive Rail Set: {volts:0.00}  counts: {counts}");
            Qa430Usb.Singleton.WriteRegister(9, counts);
        }

        internal static void SetNegativeRailVoltage(double volts)
        {
            byte counts = ComputeCountsFromVoltage(-volts);
            //Debug.WriteLine($"Negative Rail Set: {volts:0.00}  counts: {counts}");
            Qa430Usb.Singleton.WriteRegister(8, counts);
        }

        static byte ComputeCountsFromVoltage(double v)
        {
            // V = counts/256 * 2.44 * 6
            // 2.44V is DAC reference
            // 6 is opamp gain stage

            int counts = (int)Math.Round(v * 256.0 / (2.44 * 6.0));

            if (counts > 255)
                counts = 255;

            if (counts < 0)
                counts = 0;
            return (byte)counts;
        }

        internal static UInt32 SetNegInput(OpampNegInputs input)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetNegInput(input, writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
            return writeVal;
		}

		internal static UInt32 SetNegInput(OpampNegInputs input, UInt32 writeVal)
        {
            switch (input)
            {
                case OpampNegInputs.AnalyzerTo499:
                    writeVal = writeVal.BitSet((0x1 << 5) | (0x1 << 4) | (0x1 << 3) | (0x1 << 2));
                    break;
                case OpampNegInputs.GndTo4p99:
                    writeVal = writeVal.BitSet((0x1 << 4) | (0x1 << 3) | (0x1 << 2));
                    writeVal = writeVal.BitClear(0x1 << 5);
                    break;
                case OpampNegInputs.Open:
                    writeVal = writeVal.BitSet((0x1 << 3) | (0x1 << 2));
                    writeVal = writeVal.BitClear(0x1 << 4);
                    break;
                case OpampNegInputs.AnalyzerTo4p99k:
                    writeVal = writeVal.BitSet(0x1 << 2);
                    writeVal = writeVal.BitClear(0x1 << 3);
                    break;
                case OpampNegInputs.GndTo499:
                    writeVal = writeVal.BitClear(0x1 << 2);
                    break;
                default:
                    break;
            }

            return writeVal;
        }

        internal static UInt32 SetPosInput(OpampPosInputs input)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetPosInput(input, writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
            return writeVal;
		}

		internal static UInt32 SetPosInput(OpampPosInputs input, UInt32 writeVal)
        {
            switch (input)
            {
                case OpampPosInputs.Analyzer:
                    writeVal = writeVal.BitClear((0x1 << 6) | (0x1 << 7));
                    break;
                case OpampPosInputs.Gnd:
                    writeVal = writeVal.BitClear(0x1 << 6);
                    writeVal = writeVal.BitSet(0x1 << 7);
                    break;
                case OpampPosInputs.AnalyzerTo100k:
                    writeVal = writeVal.BitSet(0x1 << 6);
                    break;
                default:
                    break;
            }

            return writeVal;
        }

		internal static UInt32 SetPosNegConnect(OpampPosNegConnects input)
        {
			UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
			writeVal = SetPosNegConnect(input, writeVal);
			Qa430Usb.Singleton.WriteRegister(5, writeVal);
			return writeVal;
		}

		internal static UInt32 SetPosNegConnect(OpampPosNegConnects input, UInt32 writeVal)
        {
            switch (input)
            {
                case OpampPosNegConnects.R49p9:
                    writeVal = writeVal.BitSet(0x1 << 1);
                    writeVal = writeVal.BitClear(0x1 << 0);
                    break;

                case OpampPosNegConnects.Short:
                    writeVal = writeVal.BitSet((0x1 << 1) | (0x1 << 0));
                    break;

                case OpampPosNegConnects.Open:
                    writeVal = writeVal.BitClear(0x1 << 1);
                    break;

                default:
                    break;
            }

            return writeVal;
        }

        internal static UInt32 SetFeedback(OpampFeedbacks input)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetFeedback(input, writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
            return writeVal;
        }


		internal static UInt32 SetFeedback(OpampFeedbacks input, UInt32 writeVal)
        {
            switch (input)
            {
                case OpampFeedbacks.R4p99k:
                    writeVal = writeVal.BitClear(0x1 << 8);
                    break;

                case OpampFeedbacks.Short:
                    writeVal = writeVal.BitSet(0x1 << 8);
                    break;

                default:
                    break;
            }

            return writeVal;
        }

        internal static UInt32 SetLoad(LoadOptions load)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetLoad(load, writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
            return writeVal;
		}

		internal static UInt32 SetLoad(LoadOptions load, UInt32 writeVal)
        {
            UInt32 loadMask = (0x1 << 9) + (0x1 << 10) + (0x1 << 11);

            writeVal = writeVal & ~loadMask;

            switch (load)
            {
                case LoadOptions.Open:
                    // Noting to do as we already cleared the mask values above
                    break;

                case LoadOptions.R2000:
                    writeVal = writeVal.BitSet(0x1 << 9);
                    break;

                case LoadOptions.R604:
                    writeVal = writeVal.BitSet(0x1 << 10);
                    break;

                case LoadOptions.R330:
                    writeVal = writeVal.BitSet(0x1 << 11);
                    break;
            }

            return writeVal;
        }

        internal  static void SetLoadNow(LoadOptions load)
        {
            UInt32 writeVal = Qa430Usb.Singleton.ReadRegister(5);
            writeVal = SetLoad(load, writeVal);
            Qa430Usb.Singleton.WriteRegister(5, writeVal);
        }

        internal static double GetLoadImpedance(LoadOptions load)
        {
            switch (load)
            {
                case LoadOptions.Open:
                    return double.PositiveInfinity;
                case LoadOptions.R2000:
                    return 2000;
                case LoadOptions.R604:
                    return 604;
                case LoadOptions.R330:
                    return 470;
                default:
                    break;
            }

            throw new Exception("Unexpected value in GetLoadImpedance()");
        }
    }
}
