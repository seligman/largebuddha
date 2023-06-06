#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <math.h>

// #pragma GCC diagnostic ignored "-Wpointer-to-int-cast"
// #pragma GCC diagnostic ignored "-Wint-to-pointer-cast"
// #pragma GCC diagnostic error "-Wimplicit-function-declaration"

static PyObject * mandelbrot_calc(PyObject *self, PyObject * args) {
    /*
        Calculate one point, doesn't use cache, points should be in natural coords
        Returns (in_set, escaped_at, escape_dist)
    */

    int isJulia, maxIters;
    double x, y, juliaX, juliaY;

    if(!PyArg_ParseTuple(args, "ddpddi", &x, &y, &isJulia, &juliaX, &juliaY, &maxIters)) {
        return NULL;
    }

    double u, v;

    if (isJulia == 0) {
        double p = sqrt(((x - 1.0 / 4.0) * (x - 1.0 / 4.0)) + (y*y));
        if (x <= p - (2.0 * (p * p)) + (1.0 / 4.0)) {
            /* This point is in the main cardioid */
            return Py_BuildValue("iid", 1, 0, 0.0);
        } else if ((x + 1.0) * (x + 1.0) + (y * y) <= 1.0 / 16.0) {
            /* This point is in the first circular bulb */
            return Py_BuildValue("iid", 1, 0, 0.0);
        }

        u = x;
        v = y;
    } else {
        u = juliaX;
        v = juliaY;
    }

    for (int i = 0; i < maxIters; i++) {
        double nextX, nextY, dist;
        nextX = x * x - y * y + u;
        nextY = 2.0 * x * y + v;
        x = nextX;
        y = nextY;
        dist = x * x + y * y;
        if (dist >= 25) {
            return Py_BuildValue("iid", 0, i, dist);
        }
    }
    return Py_BuildValue("iid", 1, 0, 0.0);
}

PyMODINIT_FUNC PyInit_mandelbrot_native_helper() {
    static PyMethodDef Methods[] = {
        {"calc", mandelbrot_calc, METH_VARARGS, "Calculate a pixel"},
        {NULL, NULL, 0, NULL}
    };

    static struct PyModuleDef module = {
        PyModuleDef_HEAD_INIT,
        "mandelbrot_native_helper",
        NULL,
        -1,
        Methods
    };

    return PyModule_Create(&module);
}

// PyMODINIT_FUNC initmandelbrot_native_helper(void)
// {
//     PyObject *m;

//     m = Py_InitModule("mandelbrot_native_helper", Methods);

//     if ( m == NULL )
//     {
//         return;
//     }

//     // MandelbrotError = PyErr_NewException("mandelbrot.error", NULL, NULL);
//     // Py_INCREF(MandelbrotError);
//     // PyModule_AddObject(m, "error", MandelbrotError);
// }

// int main(int argc, char *argv[])
// {
//     Py_Initialize();
//     PyInit_initmandelbrot_native_helper();

//     return 0;
// }

