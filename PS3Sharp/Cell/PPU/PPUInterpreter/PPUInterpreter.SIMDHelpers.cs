using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace PS3Sharp.Cell.PPU.PPUInterpreter {
    public static unsafe partial class PPUInterpreter {
        // SIMD helpers
        // Ideally use Vector128, if the API does not have the needed operation
        // then use explicit SSE/SSE2 after checking if it is available,
        // otherwise fallback to scalar 

        public static void V128ADD<T>(T* va, T* vb, T* vd) where T : unmanaged {
            Vector128<T> a = Vector128.Load(va);
            Vector128<T> b = Vector128.Load(vb);
            Vector128.Store(Vector128.Add(a, b), vd);
        }

        public static void V128SUB<T>(T* va, T* vb, T* vd) where T : unmanaged {
            Vector128<T> a = Vector128.Load(va);
            Vector128<T> b = Vector128.Load(vb);
            Vector128.Store(Vector128.Subtract(a, b), vd);
        }

        public static void V128Mul<T>(T* va, T* vb, T* vd) where T : unmanaged {
            Vector128<T> a = Vector128.Load(va);
            Vector128<T> b = Vector128.Load(vb);
            Vector128.Store(Vector128.Multiply(a, b), vd);
        }

        public static void V128MADD(float* va, float* vb, float* vc, float* vd) {
            Vector128<float> a = Vector128.Load(va);
            Vector128<float> b = Vector128.Load(vb);
            Vector128<float> c = Vector128.Load(vc);

            // vd = va * vc + vb
            Vector128.Store(Vector128.FusedMultiplyAdd(a, c, b), vd);
        }

        public static void V128AND(byte* va, byte* vb, byte* vd) {
            Vector128<byte> a = Vector128.Load(va);
            Vector128<byte> b = Vector128.Load(vb);
            Vector128.Store(Vector128.BitwiseAnd(a, b), vd);
        }

        public static void V128ANDC(byte* va, byte* vb, byte* vd) {
            Vector128<byte> a = Vector128.Load(va);
            Vector128<byte> b = Vector128.Load(vb);
            Vector128.Store(Vector128.AndNot(a, b), vd);
        }

        public static void V128OR(byte* va, byte* vb, byte* vd) {
            Vector128<byte> a = Vector128.Load(va);
            Vector128<byte> b = Vector128.Load(vb);
            Vector128.Store(Vector128.BitwiseOr(a, b), vd);
        }

        public static void V128NOR(byte* va, byte* vb, byte* vd) {
            Vector128<byte> a = Vector128.Load(va);
            Vector128<byte> b = Vector128.Load(vb);

            // vd = ~(va | vb)

            // OR the vectors then XOR with all 1s which is equivalent to (NOT)
            Vector128<byte> allOnes = Vector128.Create<byte>(0xFF);
            Vector128<byte> result = Vector128.Xor(Vector128.BitwiseOr(a, b), allOnes);
            Vector128.Store(result, vd);
        }

        public static void V128XOR(byte* va, byte* vb, byte* vd) {
            Vector128<byte> a = Vector128.Load(va);
            Vector128<byte> b = Vector128.Load(vb);
            Vector128.Store(Vector128.Xor(a, b), vd);
        }

        public static void V128SPLT_Gen<T>(T* vd, T val) where T : unmanaged {
            Vector128.Store(Vector128.Create(val), vd);
        }

        public static void V128NMSUB(float* va, float* vb, float* vc, float* vd) {
            Vector128<float> a = Vector128.Load(va);
            Vector128<float> b = Vector128.Load(vb);
            Vector128<float> c = Vector128.Load(vc);

            // vd = - (va * vc - vb)
            // expands to: vb + (-va * vc)
            Vector128.Store(Vector128.FusedMultiplyAdd(-a, c, b), vd);

            //Note: Fma.MultiplySubtractNegated should not be used here, it computes  -(a * b) - c, not -(a * b - c)
        }

        public static void V128REFP(float* vb, float* vd) {
            // Seems that Vector128 API does not provide Reciprocal function

            if (Sse.IsSupported) {
                Vector128<float> b = Vector128.Load(vb);
                Vector128.Store(Sse.Reciprocal(b), vd);
            } else {
                // Scalar fallback
                vd[0] = 1.0f / vb[0];
                vd[1] = 1.0f / vb[1];
                vd[2] = 1.0f / vb[2];
                vd[3] = 1.0f / vb[3];
            }
        }

        public static void V128RSQRTEFP(float* vb, float* vd) {
            // Seems that Vector128 API does not provide ReciprocalSqrt function

            if (Sse.IsSupported) {
                Vector128<float> b = Sse.LoadVector128(vb);
                Vector128.Store(Sse.ReciprocalSqrt(b), vd);

            } else {
                // Scalar fallback
                vd[0] = 1.0f / float.Sqrt(vb[0]);
                vd[1] = 1.0f / float.Sqrt(vb[1]);
                vd[2] = 1.0f / float.Sqrt(vb[2]);
                vd[3] = 1.0f / float.Sqrt(vb[3]);
            }
        }


        // Compare Helpers
        public static void V128CMPEQUW(uint* va, uint* vb, uint* vd) {
            if (Sse2.IsSupported) {
                Vector128<uint> a = Vector128.Load(va);
                Vector128<uint> b = Vector128.Load(vb);
                Vector128<uint> result = Sse2.CompareEqual(a, b);
                Vector128.Store(result, vd);

            } else {
                // Scalar fallback
                CMPEQScalar<uint>(va, vb, vd);
            }
        }

        public static void V128CMPEQFW(float* va, float* vb, float* vd) {
            if (Sse2.IsSupported) {
                Vector128<float> a = Vector128.Load(va);
                Vector128<float> b = Vector128.Load(vb);
                Vector128<float> result = Sse2.CompareEqual(a, b);
                Vector128.Store(result, vd);

            } else {
                // Scalar fallback
                CMPEQScalar<float>(va, vb, (uint*)vd);
            }
        }

        public static void CMPEQScalar<T>(T* va, T* vb, uint* vd) where T : unmanaged, INumber<T> {
            const uint equal = 0xFFFFFFFF;
            const uint notEqual = 0x00000000;

            vd[0] = va[0] == vb[0] ? equal : notEqual;
            vd[1] = va[1] == vb[1] ? equal : notEqual;
            vd[2] = va[2] == vb[2] ? equal : notEqual;
            vd[3] = va[3] == vb[3] ? equal : notEqual;
        }

        public static void V128CMPGTFP(float* va, float* vb, float* vd) {
            if (Sse2.IsSupported) {
                Vector128<float> a = Vector128.Load(va);
                Vector128<float> b = Vector128.Load(vb);
                Vector128<float> result = Sse2.CompareGreaterThan(a, b);
                Vector128.Store(result, vd);

            } else {
                // Scalar fallback
                CMPGTScalar<float>(va, vb, (uint*)vd);
            }
        }

        public static void CMPGTScalar<T>(T* va, T* vb, uint* vd) where T : unmanaged, INumber<T> {
            const uint equal = 0xFFFFFFFF;
            const uint notEqual = 0x00000000;

            vd[0] = va[0] > vb[0] ? equal : notEqual;
            vd[1] = va[1] > vb[1] ? equal : notEqual;
            vd[2] = va[2] > vb[2] ? equal : notEqual;
            vd[3] = va[3] > vb[3] ? equal : notEqual;
        }

        public static void V128CMPGEFP(float* va, float* vb, float* vd) {
            if (Sse2.IsSupported) {
                Vector128<float> a = Vector128.Load(va);
                Vector128<float> b = Vector128.Load(vb);
                Vector128<float> result = Sse2.CompareGreaterThanOrEqual(a, b);
                Vector128.Store(result, vd);

            } else {
                // Scalar fallback
                CMPGEScalar<float>(va, vb, (uint*)vd);
            }
        }

        public static void CMPGEScalar<T>(T* va, T* vb, uint* vd) where T : unmanaged, INumber<T> {
            const uint equal = 0xFFFFFFFF;
            const uint notEqual = 0x00000000;

            vd[0] = va[0] >= vb[0] ? equal : notEqual;
            vd[1] = va[1] >= vb[1] ? equal : notEqual;
            vd[2] = va[2] >= vb[2] ? equal : notEqual;
            vd[3] = va[3] >= vb[3] ? equal : notEqual;
        }

        public static void V128MAXFP(float* va, float* vb, float* vd) {
            if (Sse2.IsSupported) {
                Vector128<float> a = Vector128.Load(va);
                Vector128<float> b = Vector128.Load(vb);
                Vector128<float> result = Sse2.Max(a, b);
                Vector128.Store(result, vd);

            } else {
                // Scalar fallback
                MAXScalar(va, vb, vd);;
            }
        }

        public static void MAXScalar<T>(T* va, T* vb, T* vd) where T : unmanaged, INumber<T> {
            vd[0] = va[0] > vb[0] ? va[0] : vb[0];
            vd[1] = va[1] > vb[1] ? va[1] : vb[1];
            vd[2] = va[2] > vb[2] ? va[2] : vb[2];
            vd[3] = va[3] > vb[3] ? va[3] : vb[3];
        }


        public static void V128MINFP(float* va, float* vb, float* vd) {
            if (Sse2.IsSupported) {
                Vector128<float> a = Vector128.Load(va);
                Vector128<float> b = Vector128.Load(vb);
                Vector128<float> result = Sse2.Min(a, b);
                Vector128.Store(result, vd);

            } else {
                // Scalar fallback
                MINScalar(va, vb, vd);
            }
        }

        public static void MINScalar<T>(T* va, T* vb, T* vd) where T : unmanaged, INumber<T> {
            vd[0] = va[0] < vb[0] ? va[0] : vb[0];
            vd[1] = va[1] < vb[1] ? va[1] : vb[1];
            vd[2] = va[2] < vb[2] ? va[2] : vb[2];
            vd[3] = va[3] < vb[3] ? va[3] : vb[3];
        }
    }
}
