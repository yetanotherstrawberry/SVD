using MathNet.Numerics.LinearAlgebra;

namespace SVD.Helpers;

internal static class ArrayHelper
{

    /// <summary>
    /// Casts byte[,] to double[,].
    /// </summary>
    /// <param name="input">Array to be casted.</param>
    /// <returns>Casted array.</returns>
    public static double[,] ToDoubleArray(this byte[,] input)
    {
        var ret = new double[input.GetLength(0), input.GetLength(1)];
        for (int row = 0; row < ret.GetLength(0); row++)
        {
            for (int col = 0; col < ret.GetLength(1); col++)
            {
                ret[row, col] = input[row, col];
            }
        }
        return ret;
    }

    /// <summary>
    /// Casts double[,] to byte[,].
    /// </summary>
    /// <param name="input">Array to be casted.</param>
    /// <returns>Casted array.</returns>
    public static byte[,] ToByteArray(this double[,] input)
    {
        var ret = new byte[input.GetLength(0), input.GetLength(1)];
        for (int row = 0; row < ret.GetLength(0); row++)
        {
            for (int col = 0; col < ret.GetLength(1); col++)
            {
                ret[row, col] = (byte)input[row, col];
            }
        }
        return ret;
    }

    /// <summary>
    /// Casts the <c>input</c> array to <c>double</c> and performs SVD. Numerical precision is limited.
    /// </summary>
    /// <param name="input">Array to be decomposed.</param>
    /// <returns>3 objects of type <c>Matrix</c>, such that <c>U</c> * <c>W</c> * <c>VT</c> = <c>input</c>.</returns>
    public static (Matrix<double> U, Matrix<double> W, Matrix<double> VT) ToDoubleSVD(this byte[,] input)
    {
        var mat = Matrix<double>.Build.DenseOfArray(input.ToDoubleArray());
        var svd = mat.Svd();
        return (svd.U, svd.W, svd.VT);
    }

    /// <summary>
    /// Composes 3 <c>Matrix</c> objects that were result of the SVD and casts the result to byte[,]. Numerical precision is limited.
    /// </summary>
    /// <param name="tupleSVD"><c>(U, W, VT)</c>c> from <c>ToDoubleSVD</c>.</param>
    /// <returns>Composed and casted matrix.</returns>
    public static byte[,] ComposeSVD((Matrix<double> U, Matrix<double> W, Matrix<double> VT) tupleSVD)
    {
        var composed = tupleSVD.U * tupleSVD.W * tupleSVD.VT;
        return composed.ToArray().ToByteArray();
    }

}
