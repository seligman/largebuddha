#ifdef PIXELHELPER_EXPORTS
#define PIXELHELPER_API __declspec(dllexport)
#else
#define PIXELHELPER_API __declspec(dllimport)
#endif

PIXELHELPER_API int fnPixelHelper(void);
