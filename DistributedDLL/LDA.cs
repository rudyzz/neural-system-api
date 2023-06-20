/// Final Project Code
/// Written by Yudi Zhang

using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedDLL
{
    public class LDA
    {
        private const double intercept = -139.62552072779096;

        private readonly double[] weights = new double[]
        {
            -0.0004274874537012175, -7.112476079241956e-05, -6.032959839408913e-06,
            0.0017236899182181465, -0.00027467229756838476, 0.0008408920579708234,
            0.011284679072300604, 2.4820556024118505e-05, -0.00561732713102837,
            0.001552301835448457, -3.20014209068097e-05, -0.0013774190265263206,
            0.0014967221362671157, 0.00022566880846282294, 0.00014969082184377257,
            3.0949515530850246e-05, 0.00025939536182228605, 0.00030073890565505664,
            0.0025584100031245728, -0.00037353665671915957, 9.484862031694263e-06,
            0.018875918436195176, 0.0003179758670722445, 0.00012243121828003635,
            9.653655737962573e-05, 0.0003081066964932389, 0.009887059090476857,
            -0.00018682024070770104, -0.002547966973128804, 0.0001491605533530834,
            0.0002658494957851315, 0.01874263756761524, 0.007032314614278789,
            0.0009323163756579392, 0.0015206391769924917, 0.007709972745222714,
            0.007793945489414023, 0.0029525225588230984, 0.007799791506946535,
            0.0013812828812906277, 0.0004119082278350089, -0.012757105962411436,
            0.00745834902512486, -0.0005275132885360506, 0.015610064726819497,
            0.003793530709469344
        };

        public bool seizurePositive(double[] signal)
        {
            if (signal.Length != 178)
            {
                return false;
            }
            double sum = 0;
            double[] signalComplex = new double[178];
            Fourier.Forward(signal, signalComplex, FourierOptions.NoScaling);

            for (int i = 0; i <= 45; i++)
            {
                double psd = Math.Sqrt(signal[i] * signal[i] + signalComplex[i] * signalComplex[i]);
                sum += weights[i] * psd;
            }

            if ((sum + intercept) > 0)
            {
                return true;
            } else
            {
                return false;
            }
        }
        
    }
}
