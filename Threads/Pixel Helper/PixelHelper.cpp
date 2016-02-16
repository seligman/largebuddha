#ifdef WIN32
#include "stdafx.h"
#include "PixelHelper.h"
#else
// Some attempt to make a Mono compatible DLL.  This may not work anymore
/*

g++ -c -fPIC PixelHelper.cpp -o PixelHelper.o
g++ -shared -o PixelHelper.dll PixelHelper.o

LD_LIBRARY_PATH=. mono MandelThreads.exe

*/
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <string.h>
#include <pthread.h>

#define PIXELHELPER_API 
#endif

// Maps from a C# WorkItem structure
struct WorkItem
{
    int X; // The pixel to calculate
    int Y;

    // Not used in Native
    double Perc;

    int WorkType;
};

// Maps from a C# MandelState structure
struct MandelState
{
    int alias;
    int pointCount;
    int curAnti;
    int width;
    int height;
    double centerX;
    double centerY;
    double size;
    double rotate;
    double addX;
    double addY;
};

#define RenderModesBuddhabrot		1
#define RenderModesAntiBuddhabrot	2
#define RenderModesStarField		3
#define RenderModesMandelbrot		4

// To use a single critical section instead of InterlockedExchange, undefine this.  The critical section is slower
// but more 'correct' on what it's doing.  The InterlockedExchange is faster, but can suffer if too
// many worker threads try to update the same pixel at once.
// #define USE_CRIT_SECTION

#ifdef USE_CRIT_SECTION
#ifdef WIN32
CRITICAL_SECTION crit;

#define EnterCrit() EnterCriticalSection(&crit)
#define LeaveCrit() LeaveCriticalSection(&crit)
#else
pthread_mutex_t crit;

#define EnterCrit() pthread_mutex_lock(&crit)
#define LeaveCrit() pthread_mutex_unlock(&crit)
#endif
#else
#define EnterCrit() 
#define LeaveCrit() 
#endif

// Initialize any global state
extern "C" PIXELHELPER_API void PH_InitPixelHelper()
{
#ifdef USE_CRIT_SECTION
#ifdef WIN32
    InitializeCriticalSection(&crit);
#else
    pthread_mutex_init(&crit, NULL);
#endif
#endif
    srand((unsigned int)time(NULL));
}

// Internal helper to return a double, maps to .NET's Random.NextDouble()
double rndNextDouble()
{
    return ((double)((rand() << 15) | rand())) / ((double)0x3FFFFFFF);
}

// The Chroma functions are helpers for picking a color on a Mandel render
double ChromaRange(double cur, double min, double max)
{
    return (cur - min) / (max - min);
}

// The Chroma functions are helpers for picking a color on a Mandel render
double ChromaRed(double cur)
{
    if ((cur >= 0.0) && (cur <= 22.0))
    {
        return ChromaRange(cur, 0.0, 22.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 22.0) && (cur <= 44.0))
    {
        return (1.0 - ChromaRange(cur, 22.0, 44.0)) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 44.0) && (cur <= 88.0))
    {
        return 48.0;
    }
    else if ((cur >= 88.0) && (cur <= 110.0))
    {
        return ChromaRange(cur, 88.0, 110.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 110.0) && (cur <= 154.0))
    {
        return 224.0;
    }
    else if ((cur >= 154.0) && (cur <= 177.0))
    {
        return (1.0 - ChromaRange(cur, 154.0, 177.0)) * (224.0 - 40.0) + 40.0;
    }
    else if ((cur >= 177.0) && (cur <= 200.0))
    {
        return ChromaRange(cur, 177.0, 200.0) * (224.0 - 40.0) + 40.0;
    }
    else if ((cur >= 200.0) && (cur <= 222.0))
    {
        return (1.0 - ChromaRange(cur, 200.0, 222.0)) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 222.0) && (cur <= 266.0))
    {
        return 48.0;
    }
    else if ((cur >= 266.0) && (cur <= 288.0))
    {
        return ChromaRange(cur, 266.0, 288.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 288.0) && (cur <= 332.0))
    {
        return 224.0;
    }
    else if ((cur >= 332.0) && (cur <= 355.0))
    {
        return (1.0 - ChromaRange(cur, 332.0, 355.0)) * (224.0 - 40.0) + 40.0;
    }
    return 0.0;
}

// The Chroma functions are helpers for picking a color on a Mandel render
double ChromaGreen(double cur)
{
    if ((cur >= 0.0) && (cur <= 44.0))
    {
        return 48.0;
    }
    else if ((cur >= 44.0) && (cur <= 66.0))
    {
        return ChromaRange(cur, 44.0, 66.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 66.0) && (cur <= 132.0))
    {
        return 224.0;
    }
    else if ((cur >= 132.0) && (cur <= 154.0))
    {
        return (1.0 - ChromaRange(cur, 132.0, 154.0)) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 154.0) && (cur <= 222.0))
    {
        return 48.0;
    }
    else if ((cur >= 222.0) && (cur <= 244.0))
    {
        return ChromaRange(cur, 222.0, 244.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 244.0) && (cur <= 310.0))
    {
        return 224.0;
    }
    else if ((cur >= 310.0) && (cur <= 332.0))
    {
        return (1.0 - ChromaRange(cur, 310.0, 332.0)) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 332.0) && (cur <= 355.0))
    {
        return 48.0;
    }

    return 0;
}

// The Chroma functions are helpers for picking a color on a Mandel render
double ChromaBlue(double cur)
{
    if ((cur >= 0.0) && (cur <= 22.0))
    {
        return ChromaRange(cur, 0.0, 22.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 22.0) && (cur <= 66.0))
    {
        return 224.0;
    }
    else if ((cur >= 66.0) && (cur <= 88.0))
    {
        return (1.0 - ChromaRange(cur, 66.0, 88.0)) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 88.0) && (cur <= 110.0))
    {
        return ChromaRange(cur, 88.0, 110.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 110.0) && (cur <= 132.0))
    {
        return (1.0 - ChromaRange(cur, 110.0, 132.0)) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 132.0) && (cur <= 178.0))
    {
        return 48.0;
    }
    else if ((cur >= 178.0) && (cur <= 200.0))
    {
        return ChromaRange(cur, 178.0, 200.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 200.0) && (cur <= 244.0))
    {
        return 224.0;
    }
    else if ((cur >= 244.0) && (cur <= 266.0))
    {
        return (1.0 - ChromaRange(cur, 244.0, 266.0)) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 266.0) && (cur <= 288.0))
    {
        return ChromaRange(cur, 266.0, 288.0) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 288.0) && (cur <= 310.0))
    {
        return (1.0 - ChromaRange(cur, 288.0, 310.0)) * (224.0 - 48.0) + 48.0;
    }
    else if ((cur >= 310.0) && (cur <= 355.0))
    {
        return 48.0;
    }

    return 0;
}

// Helper to help color a mandel pixel, the ChromaRed/Green/Blue functions are
// a specific color palette picked because it looks somewhat good.
void GetPointMandel(double val, double * r, double * g, double * b)
{
    if (val > 0)
    {
        while (val > 355.0)
        {
            val -= 355.0;
        }

        *r += ChromaRed(val);
        *g += ChromaGreen(val);
        *b += ChromaBlue(val);
    }
}

// Convert an X/Y point to a complex number, taking into account rotation
void EnginePointToComplex(int itemX, int itemY, MandelState * state, double * ptX, double * ptY)
{
    double x = itemX;
    double y = itemY;

    double statealias = state->alias;
    double statewidth = state->width;
    double statesize = state->size;
    int statePointX = state->curAnti % state->alias;
    int statePointY = state->curAnti / state->alias;

    double re = 2 * state->addX * statesize +
        -statealias * statesize * statewidth +
        2 * statesize * statePointX +
        2 * statealias * statesize * x;
    double im = 2 * state->addY * statesize +
        -statealias * state->height * statesize +
        2 * statesize * statePointY +
        2 * statealias * statesize * y;

    double temp = 2 * statealias * statewidth;

    re = temp * state->centerX + re;
    im = temp * state->centerY + im;

    temp = (state->rotate * 3.141592653589) / (180);

    double re2 = cos(temp);
    double im2 = sin(temp);

    temp = (1 / (2 * statealias * statewidth));

    *ptX = (re * re2 - im * im2) * temp;
    *ptY = (im * re2 + re * im2) * temp;
}

int SettingsMode = RenderModesBuddhabrot;
int SettingsIters = 100;
int SettingsWidth = 500;
int SettingsHeight = 500;
int SettingsViewOffX = 0;
int SettingsViewOffY = 0;
int SettingsViewWidth = 500;
int SettingsViewHeight = 500;

// Helper to set all setting
extern "C" PIXELHELPER_API void PH_SetSettings(
    int Mode,
    int Iters,
    int Width,
    int Height,
    int ViewOffX,
    int ViewOffY,
    int ViewWidth,
    int ViewHeight)
{
    SettingsMode = Mode;
    SettingsIters = Iters;
    SettingsWidth = Width;
    SettingsHeight = Height;
    SettingsViewOffX = ViewOffX;
    SettingsViewOffY = ViewOffY;
    SettingsViewWidth = ViewWidth;
    SettingsViewHeight = ViewHeight;
}

// Helper for debugging, not used anymore
extern "C" PIXELHELPER_API void PH_Test(
    unsigned long long * temp,
    int count)
{
    // Nothing to do
}

// Allocates and zeros out a chunk of memory
extern "C" PIXELHELPER_API void* PH_Alloc(
    unsigned long long length)
{
    void* ret = LocalAlloc(LPTR, (size_t)length);
    if (ret)
    {
        // This forces a page write, to allocate each page
        memset(ret, 0, (size_t)length);
    }
    return ret;
}

// Current working state, shared by all threads
struct CommonBuffer
{
    unsigned long long * m_levelsData;
    double * m_levelsPlotReal;
    double * m_levelsPlotImaginary;
    double * m_levelsPlotOther;
};

// Unique state per thread
struct ThreadBuffer
{
    void * current;
    void * start;
    unsigned long long left;
    unsigned long long total;

    double * pointTrailX;
    double * pointTrailY;
    int * pointTrailPtX;
    int * pointTrailPtY;
};

// Undefine this to use a memory-mapped view of a file, instead of a raw malloc.  The memory map is slower, but 
// let's us use more memory than physical RAM is available.  Though, it turns out with the Buddhabrot, that 
// causes so much disk swapping to occur that it'll basically never finish
// #define USE_MEMORY_MAP

// This is the sentinel value used to know if a given thread 'owns' a pixel for updating it
// It's not used if USE_CRIT_SECTION is set
#define OWNED_VALUE 0X7FFFFFFFFFFFFF00

#ifdef USE_MEMORY_MAP
HANDLE hFile = NULL;
HANDLE hMapFile = NULL;
#else
unsigned long long totalSize = 0;
#endif
LPVOID lpVoid = NULL;
LPVOID lpVoidOrig = NULL;
int nextThreadId = 0;

// Clean up everything
extern "C" PIXELHELPER_API void PH_CloseAll()
{
#ifdef USE_MEMORY_MAP
    if (lpVoidOrig)
    {
        UnmapViewOfFile(lpVoidOrig);
        lpVoidOrig = NULL;
    }

    if (hMapFile)
    {
        CloseHandle(hMapFile);
        hMapFile = NULL;
    }

    if (hFile)
    {
        CloseHandle(hFile);
        hFile = NULL;
    }
#else
    if (lpVoidOrig)
    {
        free(lpVoidOrig);
        lpVoidOrig = NULL;
    }
#endif
}

// Allocate an initialize all memory
extern "C" PIXELHELPER_API void * PH_InitCommon(
    bool levelsPlotReal,
    bool levelsPlotImaginary,
    bool levelsPlotOther)
{
    // We store everything in here, we may have pointers to this data elsewhere
    CommonBuffer * ret = (CommonBuffer*)PH_Alloc((unsigned long long)sizeof(CommonBuffer));

    // Calculate how big we need to allocate
    unsigned long long size = 0;
    unsigned long long wh = ((unsigned long long)SettingsViewWidth) * ((unsigned long long)SettingsViewHeight);
    // Two ints per pixel
    size += sizeof(int) * 2;
    // One UINT64 per pixel (the level)
    size += sizeof(unsigned long long) * wh;

    // If we're tracking real numbers then one double per pixel
    if (levelsPlotReal)
    {
        size += sizeof(double) * wh;
    }

    // If we're tracking imaginary numbers then one double per pixel
    if (levelsPlotImaginary)
    {
        size += sizeof(double) * wh;
    }

    // If we're tracking other data (probably the B of a RGB quad) then one double per pixel
    if (levelsPlotOther)
    {
        size += sizeof(double) * wh;
    }

    // Allocate the memory
#ifdef USE_MEMORY_MAP
    hFile = CreateFile(_T("DataDump.dat"), GENERIC_WRITE | GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_ALWAYS, NULL, NULL);
    hMapFile = CreateFileMapping(hFile, NULL, PAGE_READWRITE, size >> 32, (DWORD)size, NULL);  
    lpVoid = MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, (SIZE_T)size);
    lpVoidOrig = lpVoid;
    memset(lpVoid, 0, (SIZE_T)size);
#else
    lpVoid = PH_Alloc(size);
    lpVoidOrig = lpVoid;
    totalSize = size;
#endif

    // Set the width and height into the memory chunk we allocated
    *((int*)lpVoid) = SettingsWidth;
    lpVoid = (void*)(((unsigned long long)lpVoid) + (sizeof(int)));
    *((int*)lpVoid) = SettingsHeight;
    lpVoid = (void*)(((unsigned long long)lpVoid) + (sizeof(int)));

    // And some pointers into the data
    ret->m_levelsData = (unsigned long long*) lpVoid;
    lpVoid = (void*)(((unsigned long long)lpVoid) + (sizeof(unsigned long long) * wh));

    if (levelsPlotReal)
    {
        ret->m_levelsPlotReal = (double*)lpVoid;
        lpVoid = (void*)(((unsigned long long)lpVoid) + (sizeof(double) * wh));
    }
    else
    {
        ret->m_levelsPlotReal = NULL;
    }

    if (levelsPlotImaginary)
    {
        ret->m_levelsPlotImaginary = (double*)lpVoid;
        lpVoid = (void*)(((unsigned long long)lpVoid) + (sizeof(double) * wh));
    }
    else
    {
        ret->m_levelsPlotImaginary = NULL;
    }

    if (levelsPlotOther)
    {
        ret->m_levelsPlotOther = (double*)lpVoid;
        lpVoid = (void*)(((unsigned long long)lpVoid) + (sizeof(double) * wh));
    }
    else
    {
        ret->m_levelsPlotOther = NULL;
    }


    // Finall return the mess
    return ret;
}

// Helper to create the data necessary for a thread
extern "C" PIXELHELPER_API void * PH_InitThread()
{
    // Allocate a struct to store things
    ThreadBuffer * ret = (ThreadBuffer*)PH_Alloc((unsigned long long)sizeof(ThreadBuffer));

    if (ret)
    {
        // This is the work area, we store results here first, then when it
        // nears full, apply it all to the common area
        if (sizeof(void*) == 4)
        {
            ret->total = 50ULL * 1024ULL * 1024ULL;
        }
        else
        {
            ret->total = 500ULL * 1024ULL * 1024ULL;
        }

        // How much data is left?
        ret->left = ret->total;
        // Where do we start work?
        ret->start = PH_Alloc(ret->total);

        if (ret->start)
        {
            // Set the current offset
            ret->current = ret->start;

            // And create the trails of points to use
            ret->pointTrailX = (double*)PH_Alloc(sizeof(double) * (SettingsIters * 10));
            ret->pointTrailY = (double*)PH_Alloc(sizeof(double) * (SettingsIters * 10));

            ret->pointTrailPtX = (int*)PH_Alloc(sizeof(int) * (SettingsIters * 10));
            ret->pointTrailPtY = (int*)PH_Alloc(sizeof(int) * (SettingsIters * 10));
        }
        else
        {
            ret = NULL;
        }
    }

    return ret;
}

// Some helpers to manage pushing and popping data from the thread's storage area

// Push an int to the memory blob
#define TD_PUSH_INT(val) \
	*((int*)(thread->current)) = (val); \
	thread->left -= sizeof(int); \
	thread->current = (void*)(((unsigned long long)thread->current) + sizeof(int))
// Push a double to the memory blob
#define TD_PUSH_DOUBLE(val) \
	*((double*)(thread->current)) = (val); \
	thread->left -= sizeof(double); \
	thread->current = (void*)(((unsigned long long)thread->current) + sizeof(double))
// Give the size (in bytes) of a given number of elements to store, it's 
//  (int + 2 * double) + (int * (2 * count))
#define TD_SIZE_FOR_ELEMENTS(val) \
	(sizeof(int) + (sizeof(double) * 3) + (sizeof(int) * (2 * (val))))

// Get an int from the the current position and move forward
#define TD_GET_INT(val) \
	val = *((int*)thread->current); \
	thread->left += sizeof(int); \
	thread->current = (void*)(((unsigned long long)thread->current) + sizeof(int))
// Get a double from the current memory blob
#define TD_GET_DOUBLE(val) \
	val = *((double*)thread->current); \
	thread->left += sizeof(double); \
	thread->current = (void*)(((unsigned long long)thread->current) + sizeof(double))

// Helper to 'force' cast an INT64 to double and vice versa
#define CAST_LONG2DBL(l) *((double*)(void*)(&(l)))
#define CAST_DBL2LONG(d) *((LONG64*)(void*)(&(d)))

// Helper to perform an InterlockedIncrement on a double by abusing InterlockedCompareExchange
#define InterlockedIncrementDbl(loc, value) \
        { \
            double newCurrentValue = 0; \
            for (;;) \
                        { \
                double currentValue = newCurrentValue; \
                double newValue = currentValue + value; \
                LONG64 temp = InterlockedCompareExchange64((LONG64*)loc, CAST_DBL2LONG(newValue), CAST_DBL2LONG(currentValue)); \
                newCurrentValue = CAST_LONG2DBL(temp); \
                if (newCurrentValue == currentValue) \
                                { \
                    break; \
                                } \
                        } \
        }

// Dump the work area of the current thread to the main memory area
void PH_DumpInternal(CommonBuffer * common, ThreadBuffer * thread, BOOL toDisk)
{
    if (thread->left < thread->total)
    {
        // Move back to the start of the memory region to get all the values out of it
        thread->current = thread->start;

        if (common->m_levelsPlotReal && common->m_levelsPlotOther)
        {
            // While there's data
            while (thread->left < thread->total)
            {
                int count;
                double a;
                double b;
                double c;

                // Get the value to apply
                TD_GET_INT(count);
                TD_GET_DOUBLE(a);
                TD_GET_DOUBLE(b);
                TD_GET_DOUBLE(c);

                // And for each point to apply it to
                while (count > 0)
                {
                    int x;
                    int y;

                    // Get the point
                    TD_GET_INT(x);
                    TD_GET_INT(y);

                    // Bounds check that point
                    if (x >= SettingsViewOffX && x < SettingsViewOffX + SettingsViewWidth &&
                        y >= SettingsViewOffY && y < SettingsViewOffY + SettingsViewHeight)
                    {
                        // Calc the index of the point in question
                        unsigned long long xy = ((unsigned long long)(x - SettingsViewOffX)) + (((unsigned long long)(y - SettingsViewOffY)) * ((unsigned long long)SettingsViewWidth));

                        // Note: in each of these we're adding the final exit point of the point trail to each point along the point trail
                        // For Mandelbrot renders, this just means we're storing RGB values
                        // For Buddhabrot renders this lets us determine which 'direction' each of those points averages 
                        // out to, for coloring purposes. 
                        // In either case the level data lets us know how often it was 'hit', for brightness purposes.

#ifdef USE_CRIT_SECTION
                        // The critical section version is easy, just enter the critical section
                        // And update the memory
                        EnterCrit(x);

                        common->m_levelsData[xy]++;

                        common->m_levelsPlotReal[xy] += a;
                        common->m_levelsPlotImaginary[xy] += b;
                        common->m_levelsPlotOther[xy] += c;

                        LeaveCrit(x);
#else
                        // The interlocked version is quicker, but odder
                        for (;;)
                        {
                            // Set the levels data to the OWNED_VALUE sentinel value
                            LONGLONG levelsData = InterlockedExchange64((LONGLONG*)&common->m_levelsData[xy], OWNED_VALUE);

                            // Was the value that was already in memory not owned by another thread
                            if (levelsData != OWNED_VALUE)
                            {
                                // It was!  Go ahead and add one to the number of levels, and update
                                // the other values as necessary
                                levelsData++;
                                common->m_levelsPlotReal[xy] += a;
                                common->m_levelsPlotImaginary[xy] += b;
                                common->m_levelsPlotOther[xy] += c;

                                // And we're done, so put the correct value back in memory, we no longer "own"
                                // this pixel now
                                InterlockedExchange64((LONGLONG*)&common->m_levelsData[xy], levelsData);
                                break;
                            }
                        }
#endif
                    }

                    count--;
                }
            }
        }
        else if (common->m_levelsPlotReal)
        {
            // Same as before, just unrolled to prevent doing this outer level if check a bunch of times inside the loop
            // This is used by the Buddhabrot render
            while (thread->left < thread->total)
            {
                int count;
                double a;
                double b;
                double c;

                TD_GET_INT(count);
                TD_GET_DOUBLE(a);
                TD_GET_DOUBLE(b);
                TD_GET_DOUBLE(c);

                while (count > 0)
                {
                    int x;
                    int y;

                    TD_GET_INT(x);
                    TD_GET_INT(y);

                    if (x >= SettingsViewOffX && x < SettingsViewOffX + SettingsViewWidth &&
                        y >= SettingsViewOffY && y < SettingsViewOffY + SettingsViewHeight)
                    {
                        unsigned long long xy = ((unsigned long long)(x - SettingsViewOffX)) + (((unsigned long long)(y - SettingsViewOffY)) * ((unsigned long long)SettingsViewWidth));

#ifdef USE_CRIT_SECTION
                        EnterCrit(x);

                        common->m_levelsData[xy]++;

                        common->m_levelsPlotReal[xy] += a;
                        common->m_levelsPlotImaginary[xy] += b;

                        LeaveCrit(x);
#else
                        for (;;)
                        {
                            LONGLONG levelsData = InterlockedExchange64((LONGLONG*)&common->m_levelsData[xy], OWNED_VALUE);
                            if (levelsData != OWNED_VALUE)
                            {
                                levelsData++;
                                common->m_levelsPlotReal[xy] += a;
                                common->m_levelsPlotImaginary[xy] += b;
                                InterlockedExchange64((LONGLONG*)&common->m_levelsData[xy], levelsData);
                                break;
                            }
                    }
#endif
                }

                    count--;
            }
        }
    }
        else
        {
            // And same as the other two if clauses, but only store level data.  This is for a simple B/W 
            // Mandelbrot render

            while (thread->left < thread->total)
            {
                int count;
                double a;
                double b;
                double c;

                TD_GET_INT(count);
                TD_GET_DOUBLE(a);
                TD_GET_DOUBLE(b);
                TD_GET_DOUBLE(c);

                while (count > 0)
                {
                    int x;
                    int y;

                    TD_GET_INT(x);
                    TD_GET_INT(y);

                    if (x >= SettingsViewOffX && x < SettingsViewOffX + SettingsViewWidth &&
                        y >= SettingsViewOffY && y < SettingsViewOffY + SettingsViewHeight)
                    {
                        unsigned long long xy = ((unsigned long long)(x - SettingsViewOffX)) + (((unsigned long long)(y - SettingsViewOffY)) * ((unsigned long long)SettingsViewWidth));

#ifdef USE_CRIT_SECTION
                        EnterCrit(x);

                        common->m_levelsData[xy]++;

                        LeaveCrit(x);
#else
                        // This is different than the other two if clauses.  No need to
                        // 'own' a pixel here, we can just increment the value directly
                        // using InterlockedIncrement since we won't be touching anything
                        // else
                        InterlockedIncrement64((LONGLONG*)&common->m_levelsData[xy]);
#endif
                    }

                    count--;
                }
            }
        }

        // Reset the pointers so we can start adding data again
        thread->left = thread->total;
        thread->current = thread->start;
}

#ifndef USE_MEMORY_MAP
    // If we're not using a memory map, only dump to disk when we're told to, memory mapped
    // mode is saved to disc by the kernel magically.
    if (toDisk)
    {
        // Just open a file to write to
        HANDLE hCopy = CreateFile(_T("DataDump.dat"), GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, CREATE_ALWAYS, NULL, NULL);
        DWORD dwWrote = 0;
        unsigned long long left = totalSize;
        LPVOID ptr = lpVoidOrig;

        // And write all the data
        while (left > 0)
        {
            DWORD dwWrite = 1073741824;
            if (dwWrite > left)
            {
                dwWrite = (DWORD)left;
            }
            WriteFile(hCopy, ptr, dwWrite, &dwWrote, NULL);
            ptr = (void*)(((UINT_PTR)ptr) + dwWrote);
            left -= dwWrote;
        }

        // All done
        CloseHandle(hCopy);
    }
#endif
}

// Helper to clean up memory, and maybe write to disk if the 'final' flag is set
extern "C" PIXELHELPER_API void PH_Dump(CommonBuffer * common, ThreadBuffer * thread, BOOL final)
{
#ifdef USE_MEMORY_MAP
    PH_DumpInternal(common, thread, TRUE);
#else
    PH_DumpInternal(common, thread, final);
#endif
}

// The main worker, calculates for a potentional pixel, for buddha, might touch every other
// pixel in memory
extern "C" PIXELHELPER_API void PH_WorkerCalcPixel(
    WorkItem * item,
    MandelState * state,
    CommonBuffer * common,
    ThreadBuffer * thread)
{
    int mode = SettingsMode;

    // This one worker will calculate the same pixel a bunch of times
    for (state->curAnti = 0; state->curAnti < state->pointCount; state->curAnti++)
    {
        // Add some random fuzzing to prevent morie effects
        state->addX = rndNextDouble();
        state->addY = rndNextDouble();

        int pointTrailC = 0;
        int pointTrailPtC = 0;

        double ptX;
        double ptY;
        EnginePointToComplex(item->X, item->Y, state, &ptX, &ptY);
        bool inSet = true;
        int iters = SettingsIters;
        int width = SettingsWidth;
        int height = SettingsHeight;
        int periodLoop = 0;

        double curX = 0;
        double curY = 0;

        // For some modes, save a bunch of time by preventing any calculations
        // when 'inside' the mandelbrot set, since they'd just be tossed
        // away anyway
        bool inPeriod = false;
        if (mode == RenderModesBuddhabrot ||
            mode == RenderModesStarField ||
            mode == RenderModesMandelbrot)
        {
            if (!inPeriod)
            {
                double p = sqrt(((ptX - 0.25) * (ptX - 0.25)) + (ptY * ptY));
                if (ptX < p - 2 * p * p + 0.25)
                {
                    inPeriod = true;
                }
            }

            if (!inPeriod)
            {
                if (((ptX + 1) * (ptX + 1)) + ptY * ptY < 0.0625)
                {
                    inPeriod = true;
                }
            }
        }

        int bailAt = 0;
        double bailX = 0;
        double bailY = 0;

        double r = 0;
        double g = 0;
        double b = 0;

        if (!inPeriod)
        {
            // Basic mandelbrot calculation, finally
            // Z(n+1) = Z(n)^2 + c
            int checkQuantum = 8;
            int resetCheck = checkQuantum;
            double checkLastX = -1.79769e+308;
            double checkLastY = -1.79769e+308;

            for (int iter = 0; iter < iters; iter++)
            {
                bailAt++;

                double tempX = (curX * curX - curY * curY) + ptX;
                double tempY = (curY * curX + curX * curY) + ptY;
                curX = tempX;
                curY = tempY;

                periodLoop++;
                // We keep track of all the values of Z(n) for the buddhabrot
                thread->pointTrailX[pointTrailC] = curX;
                thread->pointTrailY[pointTrailC] = curY;
                pointTrailC++;

                bailX = curX;
                bailY = curY;

                // Did the value escape?
                if ((curX * curX + curY * curY) > 25)
                {
                    inSet = false;
                    break;
                }

                // Look for loops (meaning this point won't escape, and would hit the iters limit if
                // we let it go.
                if (curX == checkLastX && curY == checkLastY)
                {
                    periodLoop++;
                    inPeriod = true;
                    break;
                }

                // Increase the size of the loop we're looking for every now and then
                resetCheck--;
                if (resetCheck == 0)
                {
                    periodLoop = 0;
                    checkLastX = curX;
                    checkLastY = curY;
                    checkQuantum += checkQuantum;
                    resetCheck = checkQuantum;
                }
            }
        }

        // Hey!  Done calculating a point.
        bool renderTrail = false;
        bool fastRender = false;
        int add = 1;

        // Determine what to do based off the render type
        if (mode == RenderModesBuddhabrot)
        {
            if (!inSet)
            {
                renderTrail = true;
            }
        }
        else if (mode == RenderModesAntiBuddhabrot)
        {
            if (inSet)
            {
                renderTrail = true;
            }
        }
        else if (mode == RenderModesStarField)
        {
            if (inSet)
            {
                renderTrail = true;
                fastRender = true;
                if (item->X >= 0 && item->Y >= 0 && item->X < width && item->Y < height)
                {
                    thread->pointTrailPtX[pointTrailPtC] = item->X;
                    thread->pointTrailPtY[pointTrailPtC] = item->Y;
                    pointTrailPtC++;
                }
            }
        }
        else if (mode == RenderModesMandelbrot)
        {
            // Simplest version, this just renders a classic mandelbrot, but with pretty colors
            renderTrail = true;
            fastRender = true;

            if (!inSet)
            {
                int iptX = item->X;
                int iptY = item->Y;

                if (iptX >= SettingsViewOffX &&
                    iptY >= SettingsViewOffY &&
                    iptX < SettingsViewOffX + SettingsViewWidth &&
                    iptY < SettingsViewOffY + SettingsViewHeight)
                {
                    thread->pointTrailPtX[pointTrailPtC] = iptX;
                    thread->pointTrailPtY[pointTrailPtC] = iptY;
                    pointTrailPtC++;
                    add = 1;

                    double partial = sqrt(curX * curX + curY * curY);
                    partial = /* log(2.0 * log(5.0)) */ 1.169032175887 - log(log(partial));
                    partial = partial / /* log(2.0) */ 0.693147180559;
                    partial = (partial > 1) ? 1 : ((partial < 0) ? 0 : partial);

                    GetPointMandel(partial + bailAt, &r, &g, &b);
                }
            }
            else
            {
                int iptX = item->X;
                int iptY = item->Y;

                if (iptX >= SettingsViewOffX &&
                    iptY >= SettingsViewOffY &&
                    iptX < SettingsViewOffX + SettingsViewWidth &&
                    iptY < SettingsViewOffY + SettingsViewHeight)
                {
                    thread->pointTrailPtX[pointTrailPtC] = iptX;
                    thread->pointTrailPtY[pointTrailPtC] = iptY;
                    pointTrailPtC++;
                }
            }
        }

        // Ok, this mode requires us to deal with the point trail, like the Buddhabrot
        if (renderTrail && !fastRender)
        {
            double strotate = state->rotate;
            double stcenterReal = state->centerX;
            double stcenterImaginary = state->centerY;
            int stwidth = state->width;
            int stheight = state->height;
            double stsize = state->size;
            int maxPTc = pointTrailC;
            int curPTc = 0;

            if (mode == RenderModesAntiBuddhabrot)
            {
                maxPTc = iters;
            }

            // Run through each point in the trail and note it, we'll apply it to the main chunk of memory later
            for (int i = 0; i < maxPTc; i++)
            {
                curPTc++;
                if (curPTc >= pointTrailC)
                {
                    curPTc -= periodLoop;
                }

                double re = thread->pointTrailX[curPTc];
                double im = thread->pointTrailY[curPTc];

                double rotx = re * cos(0.0174532925199432 * -strotate) - im * sin(0.0174532925199432 * -strotate);
                double roty = re * sin(0.0174532925199432 * -strotate) + im * cos(0.0174532925199432 * -strotate);

                double x = (stwidth * (-stcenterReal + 0.5 * stsize + rotx)) / stsize;
                double y = (0.5 * stheight * stsize - stcenterImaginary * stwidth + stwidth * roty) / stsize;

                int iptX = (int)(x + 0.5);
                int iptY = (int)(y + 0.5);

                if (iptX >= SettingsViewOffX &&
                    iptY >= SettingsViewOffY &&
                    iptX < SettingsViewOffX + SettingsViewWidth &&
                    iptY < SettingsViewOffY + SettingsViewHeight)
                {
                    thread->pointTrailPtX[pointTrailPtC] = iptX;
                    thread->pointTrailPtY[pointTrailPtC] = iptY;
                    pointTrailPtC++;
                }
            }
        }

        // And finally, push the work we did to our internal buffer.  This buffer is applied as it
        // gets full, or when it's specifically dumped just before the app closes
        if (renderTrail && pointTrailPtC > 0)
        {
            if (mode == RenderModesMandelbrot)
            {
                // Mandelbrot set is simple, just add the pixel to the buffer, along with the RGB values we
                // calculated
                int x = thread->pointTrailPtX[0];
                int y = thread->pointTrailPtY[0];

                // Dump the memory buffer when there's not enough room for one more element
                if (thread->left <= TD_SIZE_FOR_ELEMENTS(1))
                {
                    PH_DumpInternal(common, thread, FALSE);
                }

                // Just one point to update
                TD_PUSH_INT(1);

                // Store the RGB quad
                TD_PUSH_DOUBLE(r);
                TD_PUSH_DOUBLE(g);
                TD_PUSH_DOUBLE(b);

                // And the point
                TD_PUSH_INT(x);
                TD_PUSH_INT(y);
            }
            else
            {
                // This is a buddhabrot like set

                // Dump the memory buffer if there's not enough room to store this point trail
                if (thread->left <= TD_SIZE_FOR_ELEMENTS(pointTrailPtC))
                {
                    PH_DumpInternal(common, thread, FALSE);
                }

                // Save the size of the point trail
                TD_PUSH_INT(pointTrailPtC);

                // Use the RG of the RGB quad to store the final
                // exit point of the point trail, which is used to color things
                TD_PUSH_DOUBLE(ptX);
                TD_PUSH_DOUBLE(ptY);
                TD_PUSH_DOUBLE(0);

                // Add each point in the point trail
                for (int i = 0; i < pointTrailPtC; i++)
                {
                    TD_PUSH_INT(thread->pointTrailPtX[i]);
                    TD_PUSH_INT(thread->pointTrailPtY[i]);
                }
            }
        }
    }
}
