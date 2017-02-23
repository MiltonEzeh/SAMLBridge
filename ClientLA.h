// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the CLIENTLA_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// CLIENTLA_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef CLIENTLA_EXPORTS
#define CLIENTLA_API __declspec(dllexport)
#else
#define CLIENTLA_API __declspec(dllimport)
#endif

#define LA_ERR_SUCCESS 				0
#define LA_ERR_BUFFER_TOO_SMALL 	-1
#define LA_ERR_USER_CANCEL	 		-2
#define LA_ERR_TIMEOUT		 		-3
#define LA_ERR_CONFIG_ERROR			-4
#define LA_ERR_BAD_SMARTCARD		-5
#define LA_ERR_CARDLIB_FAIL			-6
#define LA_ERR_CARDLIB_NO_READER	-7
#define LA_ERR_WRONG_PIN			-8
#define LA_ERR_PIN_LOCKED			-9
#define LA_ERR_PIN_EXPIRED			-10
#define LA_ERR_P7_ERROR				-11

typedef CLIENTLA_API int _LAClientApi_SignChallenge(
    char *Challenge, char *Result, int *ResultCount);

typedef CLIENTLA_API char* _LAClientApi_LastErrorString(void);
