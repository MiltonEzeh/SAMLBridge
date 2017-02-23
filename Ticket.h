/*
 * Project  : NHS Connecting for Health
 * Subsystem: SAML Bridge
 * Component: Identity Agent Ticket API ActiveX Wrapper
 *
 * IDE      : Microsoft Visual Studio 2005
 * Solution : IATicket
 * Project  : IATicket
 * File     : Ticket.h
 *
 * Coclass  : CTicket
 * Interface: ITicket
 *
 * Author   : Peter Lucas
 * Company  : Fujitsu Services
 * Date     : April 2006
 *
 * Updated  : Nick James
 * Company  : Fujitsu Services
 * Date     : April - May 2006
 */

#pragma once
#include "resource.h"
#include "ticket_api.h"
#include "ClientLA.h"

#include <atlctl.h>
#include <ExDisp.h>
#include <shlguid.h>
#include <WinInet.h>

// ITicket
[
	object,
	uuid("D99D053E-0940-4E0D-B62F-C384E5766E3E"),
	dual,
	helpstring("ITicket Interface"),
	pointer_default(unique)
]

__interface ITicket : IDispatch
{
	[id(1), helpstring("method DestroyTicket")]
		HRESULT DestroyTicket(void);

	[id(2), helpstring("method GetGAVersion")]
		HRESULT GetGAVersion([in] LONG componentType, [out, retval] LONG* version);

	[id(3), helpstring("method GetErrorDescription")]
		HRESULT GetErrorDescription([in] LONG error, [out, retval] BSTR* description);

	[id(4), helpstring("method GetLastError")]
		HRESULT GetLastError([out, retval] LONG* errorCode);

	[id(5), helpstring("method GetNewTicket")]
		HRESULT GetNewTicket([out, retval] BSTR* tokenId);

	[id(6), helpstring("method GetTicket")]
		HRESULT GetTicket([out, retval] BSTR* tokenId);

	[id(7), helpstring("method Finalize")]
		HRESULT Finalize(void);

    [id(8), helpstring("method SignChallenge")]
        HRESULT SignChallenge(
            [in] BSTR bstrChallenge, [out, retval] BSTR* pbstrSignedChallenge);

    [id(9), helpstring("method GetLastLAError")]
        HRESULT GetLastLAError([out, retval] BSTR* pbstrLastLAError);

	[id(10), helpstring("method GetTicketNoAuth")]
		HRESULT GetTicketNoAuth([out, retval] BSTR* tokenId);

	[id(11), helpstring("method GetProductVersion")]
		HRESULT GetProductVersion([out, retval] BSTR* pbstrProductVersion);
};

// CTicket
[
	coclass,
	threading(apartment),
	support_error_info("ITicket"),
	aggregatable(never),
	vi_progid("IATicket.Ticket"),
	progid("IATicket.Ticket.1"),
	version(1.0),
	uuid("F1B17D8F-F096-47E1-A4D0-21DB9D757780"),
	helpstring("Ticket Class")
]

class ATL_NO_VTABLE CTicket
:
	public IObjectWithSiteImpl<CTicket>,
	public ITicket
{
public:
	CTicket() {}

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	BEGIN_COM_MAP(CTicket)
		COM_INTERFACE_ENTRY(IObjectWithSite)
	END_COM_MAP()

	BEGIN_CATEGORY_MAP(CTicket)
		IMPLEMENTED_CATEGORY(CATID_SafeForInitializing)
		IMPLEMENTED_CATEGORY(CATID_SafeForScripting)
	END_CATEGORY_MAP()

	HRESULT FinalConstruct();	
	void FinalRelease();

private:
    // Sizes.
    static const int SIZ_ERROR_DESCRIPTION_BUFFER = 1024;
    static const int SIZ_TICKET_BUFFER            = 254;
    static const int SIZ_EXTENDED_TICKET_BUFFER   = 1024;
    static const int SIZ_URL_SCHEME_BUFFER        = 32;

	// DLL filenames.
	static const char DLL_IA_TICKET_API[];
	static const char DLL_LA_CLIENT_API[];
	static const char DLL_IA_TICKET    [];

	// Identity Agent ticket API function names.
	static const char IAAPI_INITIALIZE          [];
	static const char IAAPI_FINALIZE            [];
	static const char IAAPI_GETGAVERSION        [];
	static const char IAAPI_GETTICKET           [];
	static const char IAAPI_GETNEWTICKET        [];
	static const char IAAPI_DESTROYTICKET       [];
	static const char IAAPI_GETLASTERROR        [];
	static const char IAAPI_GETERRORDESCRIPTION [];
	static const char IAAPI_GETTICKETNOAUTH     [];

    // Local Authenticator client API function names.
	static const char LACAPI_SIGNCHALLENGE  [];
	static const char LACAPI_LASTERRORSTRING[];

	// URL domains.
	static const char URLDOMAIN_DEVELOPMENT           [];
	static const char URLDOMAIN_UNIT_TEST             [];
	static const char URLDOMAIN_SYSTEM_TEST           [];
	static const char URLDOMAIN_INTEGRATION_TEST      [];
	static const char URLDOMAIN_CERTA                 [];
	static const char URLDOMAIN_CERTB                 [];
	static const char URLDOMAIN_MODEL_COMMUNITY       [];
	static const char URLDOMAIN_MODEL_COMMUNITY_DIRECT[];
	static const char URLDOMAIN_READY_FOR_OPERATION   [];
	static const char URLDOMAIN_READY_FOR_OPERATION_V [];
	static const char URLDOMAIN_PRODUCTION_SDC01      [];
	static const char URLDOMAIN_PRODUCTION_SDC02      [];

    // URL IP addresses.
    static const char URLIPADDR_SYSTEM_TEST     [];
	static const char URLIPADDR_INTEGRATION_TEST[];

	// Authorised host URL domains.
	static const char* m_ppszAuthorisedUrlDomains[];

	// ITicket method names.
	static const char ITICKET_FINAL_CONSTRUCT      [];
	static const char ITICKET_FINALIZE             [];
	static const char ITICKET_GET_GA_VERSION       [];
	static const char ITICKET_GET_TICKET           [];
	static const char ITICKET_GET_NEW_TICKET       [];
	static const char ITICKET_DESTROY_TICKET       [];
	static const char ITICKET_GET_LAST_ERROR       [];
	static const char ITICKET_GET_ERROR_DESCRIPTION[];
	static const char ITICKET_SIGN_CHALLENGE       [];
	static const char ITICKET_GET_LAST_LA_ERROR    [];
	static const char ITICKET_GET_TICKET_NO_AUTH   [];
	static const char ITICKET_GET_PRODUCT_VERSION  [];

	// Resource paths.
	static const TCHAR RSRCPATH_VARFILEINFO_TRANSLATION      [];
	static const TCHAR RSRCPATH_STRINGFILEINFO_PRODUCTVERSION[];

	// Masks.
	static const DWORD MASK_DWORD_WORD_0 = 0xFFFF0000;
	static const DWORD MASK_DWORD_WORD_1 = 0x0000FFFF;

	// Shifts.
	static const UINT SHIFT_DWORD_WORD_0 = 16;

	// Sizes.
	static const UINT SIZ_PRODUCT_VERSION_RESOURCE_PATH_BUFFER = 40;
	static const UINT SIZ_TRANSLATION_INFO = 4;

	// Error codes.
	static const DWORD E_FAILED_TO_LOAD_TICKET_API       = 0xE0040200;
	static const DWORD E_TICKET_API_ERROR                = 0xE0040201;
	static const DWORD E_FAILED_TO_INITIALISE_TICKET_API = 0xE0040202;
    static const DWORD E_FAILED_TO_LOAD_LA_CLIENT_API    = 0xE0040203;

	// Error messages.
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_INITIALIZE           [];
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_FINALIZE             [];
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_GETGAVERSION         [];
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_GETTICKET            [];
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_GETNEWTICKET         [];
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_DESTROYTICKET        [];
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_GETLASTERROR         [];
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_GETERRORDESCRIPTION  [];
	static const char ERRMSG_FAILED_TO_FIND_TICKETAPI_GETTICKETNOAUTH      [];
	static const char ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API      [];
	static const char ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API[];
	static const char ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED               [];
	static const char ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED         [];
	static const char ERRMSG_ERROR_DESCRIPTION_TRUNCATED                   [];
	static const char ERRMSG_TOKEN_IDENTIFIER_TRUNCATED                    [];
	static const char ERRMSG_FAILED_TO_FIND_LACAPI_SIGN_CHALLENGE          [];
	static const char ERRMSG_FAILED_TO_FIND_LACAPI_LAST_ERROR_STRING       [];
    static const char ERRMSG_FAILED_TO_LOAD_LOCAL_AUTHENTICATOR_CLIENT_API [];
    static const char ERRMSG_NO_CHALLENGE_STRING_SUPPLIED                  [];
    static const char ERRMSG_EMPTY_CHALLENGE_STRING_SUPPLIED               [];
    static const char ERRMSG_CHALLENGE_STRING_TOO_LONG                     [];
    static const char ERRMSG_LOCAL_AUTHENTICATOR_CLIENT_API_CALL_FAILED    [];
    static const char ERRMSG_FAILED_TO_COPY_SIGNED_CHALLENGE               [];
    static const char ERRMSG_FAILED_TO_COPY_LAST_ERROR_STRING              [];
    static const char ERRMSG_FAILED_TO_OBTAIN_MODULE_HANDLE                [];
    static const char ERRMSG_FAILED_TO_FIND_VERSION_INFO_RESOURCE          [];
    static const char ERRMSG_FAILED_TO_LOAD_VERSION_INFO_RESOURCE          [];
    static const char ERRMSG_FAILED_TO_LOCK_VERSION_INFO_RESOURCE          [];
    static const char ERRMSG_FAILED_TO_GET_TRANSLATION_INFORMATION         [];
    static const char ERRMSG_FAILED_TO_GET_PRODUCT_VERSION                 [];
    static const char ERRMSG_FAILED_TO_COPY_PRODUCT_VERSION                [];

	// Identity Agent API function pointers.
	_TcktApi_Initialize*          IAAPI_Initialize;
	_TcktApi_Finalize*            IAAPI_Finalize;
	_TcktApi_GetGAVersion*        IAAPI_GetGaVersion;
	_TcktApi_getTicket*           IAAPI_GetTicket;
	_TcktApi_getTicketNoAuth*     IAAPI_GetTicketNoAuth;
	_TcktApi_getNewTicket*        IAAPI_GetNewTicket;
	_TcktApi_destroyTicket*       IAAPI_DestroyTicket;
	_TcktApi_getLastError*        IAAPI_GetLastError;
	_TcktApi_getErrorDescription* IAAPI_GetErrorDescription;

    // Local Authenticator client API function pointers.
    _LAClientApi_SignChallenge*   LACAPI_SignChallenge;
    _LAClientApi_LastErrorString* LACAPI_LastErrorString;

    // Sizes.
    static const int SIZ_SIGNED_CHALLENGE_BUFFER = 30240;

    // Minima and maxima.
    static const int MAX_CHALLENGE_STRING_BYTES = 1024;

	HMODULE   m_hTicketApi;
    HMODULE   m_hLAClientApi;
	long      m_lTicketApiInstance;
	IUnknown* m_pUnkSite;

public:
	STDMETHOD (Finalize)            (void);
	STDMETHOD (GetGAVersion)        (LONG componentType, LONG* version);
	STDMETHOD (GetTicket)           (BSTR* tokenId);
	STDMETHOD (GetTicketNoAuth)     (BSTR* tokenId);
	STDMETHOD (GetNewTicket)        (BSTR* tokenId);
	STDMETHOD (DestroyTicket)       (void);
	STDMETHOD (GetLastError)        (LONG* errorCode);
	STDMETHOD (GetErrorDescription) (LONG error, BSTR* description);
    STDMETHOD (SignChallenge)       (BSTR bstrChallenge, BSTR* pbstrSignedChallenge);
    STDMETHOD (GetLastLAError)      (BSTR* pbstrLastLAError);
	STDMETHOD (GetProductVersion)   (BSTR* pbstrProductVersion);

private:
	STDMETHOD (SetSite) (IUnknown* pUnkSite);
	STDMETHOD (GetSite) (REFIID riid, LPVOID* ppvSite);

	bool            GetHostUrl             (char* pszUrl);
	INTERNET_SCHEME GetUrlScheme           (char* pszUrl);
	bool            GetUrlDomain           (char* pszUrl, char* pszBuffer, int iBufferSize);
	bool            IsDomainMatch          (char* pszRefDomain, char* pszTestDomain);
	bool            IsAuthorisedHostDomain (char* pszUrl);
	bool            IsAuthorisedHostDomain ();
};
