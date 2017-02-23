/*
 * Project  : NHS Connecting for Health
 * Subsystem: SAML Bridge
 * Component: Identity Agent Ticket API ActiveX Wrapper
 *
 * IDE      : Microsoft Visual Studio 2005
 * Solution : IATicket
 * Project  : IATicket
 * File     : Ticket.cpp
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
 *
 * This class implements a COM wrapper around the Identity Agent Ticket API as documented in
 * the BT document "External Interface Specification: Part 7 Spine Security Broker APIs".
 *
 * The Identity Agent Ticket API resides in the workstation in a DLL execution unit called
 * TicketApiDll.dll. This DLL must be placed on the system path so that the LoadLibrary call
 * does not need to specify path information.
 *
 * On a workstation a user authenticates by placing a smart card in the smart card reader.
 * The BT Identity Agent on the workstation reads the card and after the user enters their
 * PIN number, places the token ID from the card into its token store. This object will be
 * used on the redirection of the SSO attempt to obtain the token ID from the token store.
 * This same token ID is then sent from the workstation to the Spine Security Broker to
 * obtain a SAML token. This is a signed XML document which encapsulates a user's
 * authentication attributes. The SAML token is redirected from the workstation to ADFS
 * to be used for authentication.
 */

// TODO: expand error reporting, maybe by extending the range of failure HRESULTs returned.
// TODO: report error codes returned by the lower-level APIs.

#include "stdafx.h"
#include "Ticket.h"
#include "UTicketException.h"
#include "Universal.h"

// DLL filenames.
const char CTicket::DLL_IA_TICKET_API[] = _T("TicketApiDll.dll");
const char CTicket::DLL_LA_CLIENT_API[] = _T("ClientLA.dll");
const char CTicket::DLL_IA_TICKET    [] = _T("IATicket.dll");

// Identity Agent ticket API function names.
const char CTicket::IAAPI_INITIALIZE         [] = _T("_TcktApi_Initialize");
const char CTicket::IAAPI_FINALIZE           [] = _T("_TcktApi_Finalize");
const char CTicket::IAAPI_GETGAVERSION       [] = _T("_TcktApi_GetGAVersion");
const char CTicket::IAAPI_GETTICKET          [] = _T("_TcktApi_getTicket");
const char CTicket::IAAPI_GETTICKETNOAUTH    [] = _T("_TcktApi_getTicketNoAuth");
const char CTicket::IAAPI_GETNEWTICKET       [] = _T("_TcktApi_getNewTicket");
const char CTicket::IAAPI_DESTROYTICKET      [] = _T("_TcktApi_destroyTicket");
const char CTicket::IAAPI_GETLASTERROR       [] = _T("_TcktApi_getLastError");
const char CTicket::IAAPI_GETERRORDESCRIPTION[] = _T("_TcktApi_getErrorDescription");

// Local Authenticator client API function names.
const char CTicket::LACAPI_SIGNCHALLENGE  [] = _T("SignChallenge");
const char CTicket::LACAPI_LASTERRORSTRING[] = _T("LastErrorString");

// URL domains.
const char CTicket::URLDOMAIN_DEVELOPMENT           [] = _T("localhost");
const char CTicket::URLDOMAIN_UNIT_TEST             [] = _T("access.client.local");
const char CTicket::URLDOMAIN_SYSTEM_TEST           [] = _T("bstaccess.sou.ncrs.nhs.uk");
const char CTicket::URLDOMAIN_INTEGRATION_TEST      [] = _T("bitaccess.sou.ncrs.nhs.uk");
const char CTicket::URLDOMAIN_CERTA                 [] = _T("certa.sou.ncrs.nhs.uk");
const char CTicket::URLDOMAIN_CERTB                 [] = _T("certb.sou.ncrs.nhs.uk");
const char CTicket::URLDOMAIN_MODEL_COMMUNITY       [] = _T("amcaccess.sou.ncrs.nhs.uk");
const char CTicket::URLDOMAIN_MODEL_COMMUNITY_DIRECT[] = _T("amcasml01pn.fjmc.local");
const char CTicket::URLDOMAIN_READY_FOR_OPERATION   [] = _T("rfoaccess.sou.ncrs.nhs.uk");
const char CTicket::URLDOMAIN_READY_FOR_OPERATION_V [] = _T("bprnnsl01vn.fjlsp.local");
const char CTicket::URLDOMAIN_PRODUCTION_SDC01      [] = _T("01crs.sou.ncrs.nhs.uk");
const char CTicket::URLDOMAIN_PRODUCTION_SDC02      [] = _T("02crs.sou.ncrs.nhs.uk");

// URL IP addresses.
const char CTicket::URLIPADDR_SYSTEM_TEST[]      = _T("192.168.239.141");
const char CTicket::URLIPADDR_INTEGRATION_TEST[] = _T("192.168.239.142");

/*
 * CTicket::m_ppszAuthorisedHostDomains
 *
 * This item contains the list of (URL) domain names from which the IATicket ActiveX control
 * is authorised to be loaded. Each externally visible method in the control checks the host
 * URL domain against this list before it executes, in order to determine whether the call
 * should be allowed. If the host domain is not found in the list of authorised domains, the
 * method takes no action and returns E_FAIL.
 */
// TODO: remove URLDOMAIN_LOCALHOST in production
const char* CTicket::m_ppszAuthorisedUrlDomains[] =
{
    URLDOMAIN_DEVELOPMENT,
    URLDOMAIN_UNIT_TEST,
    URLDOMAIN_SYSTEM_TEST,
    URLIPADDR_SYSTEM_TEST,
    URLDOMAIN_INTEGRATION_TEST,
    URLIPADDR_INTEGRATION_TEST,
    URLDOMAIN_CERTA,
    URLDOMAIN_CERTB,
    URLDOMAIN_MODEL_COMMUNITY,
    URLDOMAIN_MODEL_COMMUNITY_DIRECT,
    URLDOMAIN_READY_FOR_OPERATION,
    URLDOMAIN_READY_FOR_OPERATION_V,
	URLDOMAIN_PRODUCTION_SDC01,
	URLDOMAIN_PRODUCTION_SDC02
};

// ITicket method names.
const char CTicket::ITICKET_FINAL_CONSTRUCT      [] = _T("ITicket::FinalConstruct(): ");
const char CTicket::ITICKET_FINALIZE             [] = _T("ITicket::Finalize(): ");
const char CTicket::ITICKET_GET_GA_VERSION       [] = _T("ITicket::GetGAVersion(): ");
const char CTicket::ITICKET_GET_TICKET           [] = _T("ITicket::GetTicket(): ");
const char CTicket::ITICKET_GET_TICKET_NO_AUTH   [] = _T("ITicket::GetTicketNoAuth(): ");
const char CTicket::ITICKET_GET_NEW_TICKET       [] = _T("ITicket::GetNewTicket(): ");
const char CTicket::ITICKET_DESTROY_TICKET       [] = _T("ITicket::DestroyTicket(): ");
const char CTicket::ITICKET_GET_LAST_ERROR       [] = _T("ITicket::GetLastError(): ");
const char CTicket::ITICKET_GET_ERROR_DESCRIPTION[] = _T("ITicket::GetErrorDescription(): ");
const char CTicket::ITICKET_SIGN_CHALLENGE       [] = _T("ITicket::SignChallenge(): ");
const char CTicket::ITICKET_GET_LAST_LA_ERROR    [] = _T("ITicket::GetLastLAError(): ");
const char CTicket::ITICKET_GET_PRODUCT_VERSION  [] = _T("ITicket::GetProductVersion(): ");

// Resource paths.
const TCHAR CTicket::RSRCPATH_VARFILEINFO_TRANSLATION[] = _T("\\VarFileInfo\\Translation");

const TCHAR CTicket::RSRCPATH_STRINGFILEINFO_PRODUCTVERSION[] =
	_T("\\StringFileInfo\\%04x%04x\\ProductVersion");

// Error messages.
const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_INITIALIZE[] =
	_T("failed to find function _TcktApi_Initialize in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_FINALIZE[] =
	_T("failed to find function _TcktApi_Finalize in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_GETGAVERSION[] =
	_T("failed to find function _TcktApi_GetGAVersion in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_GETTICKET[] =
	_T("failed to find function _TcktApi_getTicket in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_GETTICKETNOAUTH[] =
	_T("failed to find function _TcktApi_getTicketNoAuth in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_GETNEWTICKET[] =
	_T("failed to find function _TcktApi_getNewTicket in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_DESTROYTICKET[] =
	_T("failed to find function _TcktApi_destroyTicket in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_GETLASTERROR[] =
	_T("failed to find function _TcktApi_getLastError in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_FIND_TICKETAPI_GETERRORDESCRIPTION[] =
	_T("failed to find function _TcktApi_getErrorDescription in IA ticket API");

const char CTicket::ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API[] =
	_T("failed to load Identity Agent ticket API");

const char CTicket::ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API[] =
	_T("failed to initialise Identity Agent ticket API");

const char CTicket::ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED[] =
	_T("host URL domain is unauthorised");

const char CTicket::ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED[] =
	_T("IA ticket API call failed");

const char CTicket::ERRMSG_ERROR_DESCRIPTION_TRUNCATED[] =
	_T("error description has been truncated");

const char CTicket::ERRMSG_TOKEN_IDENTIFIER_TRUNCATED[] =
	_T("token identifier has been truncated");

const char CTicket::ERRMSG_FAILED_TO_FIND_LACAPI_SIGN_CHALLENGE[] =
    _T("failed to find function SignChallenge in LA client API");

const char CTicket::ERRMSG_FAILED_TO_FIND_LACAPI_LAST_ERROR_STRING[] =
    _T("failed to find function LastErrorString in LA client API");

const char CTicket::ERRMSG_FAILED_TO_LOAD_LOCAL_AUTHENTICATOR_CLIENT_API[] =
    _T("failed to load Local Authenticator client API");

const char CTicket::ERRMSG_NO_CHALLENGE_STRING_SUPPLIED[] =
    _T("no challenge string supplied to SignChallenge function");

const char CTicket::ERRMSG_EMPTY_CHALLENGE_STRING_SUPPLIED[] =
    _T("empty challenge string supplied");

const char CTicket::ERRMSG_CHALLENGE_STRING_TOO_LONG[] =
    _T("supplied challenge string is too long");

const char CTicket::ERRMSG_LOCAL_AUTHENTICATOR_CLIENT_API_CALL_FAILED[] =
    _T("LA client API call failed");

const char CTicket::ERRMSG_FAILED_TO_COPY_SIGNED_CHALLENGE[] =
    _T("failed to copy signed challenge");

const char CTicket::ERRMSG_FAILED_TO_COPY_LAST_ERROR_STRING[] =
    _T("failed to copy last error string");

const char CTicket::ERRMSG_FAILED_TO_OBTAIN_MODULE_HANDLE[] =
	_T("failed to obtain a handle to the executable module");

const char CTicket::ERRMSG_FAILED_TO_FIND_VERSION_INFO_RESOURCE[] =
	_T("failed to find to the executable's version info resource");

const char CTicket::ERRMSG_FAILED_TO_LOAD_VERSION_INFO_RESOURCE[] =
	_T("failed to load to the executable's version info resource");

const char CTicket::ERRMSG_FAILED_TO_LOCK_VERSION_INFO_RESOURCE[] =
	_T("failed to lock to the executable's version info resource");

const char CTicket::ERRMSG_FAILED_TO_GET_TRANSLATION_INFORMATION[] =
	_T("failed to get the translation information from the version info resource");

const char CTicket::ERRMSG_FAILED_TO_GET_PRODUCT_VERSION[] =
	_T("failed to get the product version from the version info resource");

const char CTicket::ERRMSG_FAILED_TO_COPY_PRODUCT_VERSION[] =
    _T("failed to copy product version");

/*
 * CTicket::FinalConstruct()
 *
 * This method is called as a result of a client call to CoCreateInstance(). It is the last
 * method called before returning to the client code. If an error occurs in this method, the
 * HRESULT setting will be returned directly to CoCreateInstance(). Any error messages
 * reported using AtlReportError() will be seen as the reason why CoCreateInstance() failed.
 */
HRESULT CTicket::FinalConstruct()
{
	HRESULT hr = S_OK;
	m_lTicketApiInstance = 0;

	try
	{
		/*
		 * Load the ticket API DLL. If this succeeds, a module handle is returned which is
		 * stored in m_hTicketApi. If the load fails, NULL is returned. Note that we do not
		 * fail the ActiveX object creation in this case, because we want the client to be
		 * able to detect the condition when it makes a call to one of the ticket APIs,
		 * rather than when the object is loaded. This allows the client to take alternative
		 * action.
		 */
		m_hTicketApi = LoadLibrary(DLL_IA_TICKET_API);

		/*
		 * If the ticket API DLL was successfully loaded, set up the function pointers to the
		 * API fuctions. The object creation is aborted if any of the expected function pointers
		 * cannot be obtained, since this represents a broken DLL situation, which should be
		 * flagged.
		 */
		if (m_hTicketApi != NULL)
		{
			IAAPI_Initialize =
				(_TcktApi_Initialize*) GetProcAddress(m_hTicketApi, IAAPI_INITIALIZE);

			if (IAAPI_Initialize == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_INITIALIZE));

			IAAPI_Finalize =
				(_TcktApi_Finalize*) GetProcAddress(m_hTicketApi, IAAPI_FINALIZE);

			if (IAAPI_Finalize == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_FINALIZE));

			IAAPI_GetGaVersion =
				(_TcktApi_GetGAVersion*) GetProcAddress(m_hTicketApi, IAAPI_GETGAVERSION);

			if (IAAPI_GetGaVersion == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_GETGAVERSION));

			IAAPI_GetTicket =
				(_TcktApi_getTicket*) GetProcAddress(m_hTicketApi, IAAPI_GETTICKET);

			if (IAAPI_GetTicket == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_GETTICKET));

			IAAPI_GetTicketNoAuth =
				(_TcktApi_getTicketNoAuth*) GetProcAddress(m_hTicketApi, IAAPI_GETTICKETNOAUTH);

			if (IAAPI_GetTicketNoAuth == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_GETTICKETNOAUTH));

			IAAPI_GetNewTicket =
				(_TcktApi_getNewTicket*) GetProcAddress(m_hTicketApi, IAAPI_GETNEWTICKET);

			if (IAAPI_GetNewTicket == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_GETNEWTICKET));

			IAAPI_DestroyTicket =
				(_TcktApi_destroyTicket*) GetProcAddress(m_hTicketApi, IAAPI_DESTROYTICKET);

			if (IAAPI_DestroyTicket == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_DESTROYTICKET));

			IAAPI_GetLastError =
				(_TcktApi_getLastError*) GetProcAddress(m_hTicketApi, IAAPI_GETLASTERROR);

			if (IAAPI_GetLastError == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_GETLASTERROR));

			IAAPI_GetErrorDescription =
				(_TcktApi_getErrorDescription*) GetProcAddress(
					m_hTicketApi, IAAPI_GETERRORDESCRIPTION);

			if (IAAPI_GetErrorDescription == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_TICKETAPI_GETERRORDESCRIPTION));

			/*
			 * Initialise the ticket API by calling the _TcktApi_Initialize() API function. If
			 * this call succeeds, an instance handle is returned which is stored in
			 * m_lTicketApiInstance. If the call fails, zero is returned. Note that we do not
			 * fail the ActiveX object creation in this case, because we want the client to be
			 * able to detect the condition when it makes a call to one of the ticket APIs,
			 * rather than when the object is loaded. This allows the client to take alternative
			 * action.
			 */
			m_lTicketApiInstance = IAAPI_Initialize();
		}

		/*
		 * Load the Local Authenticator client API DLL. If this succeeds, a module handle
         * is returned which is stored in m_hLAClientApi. If the load fails, NULL is
         * returned. Note that we do not fail the ActiveX object creation in this case,
         * because we want the client to be able to detect the condition when it makes a
         * call to one of the client APIs, rather than when the object is loaded. This
         * allows the client to take alternative action.
		 */
		m_hLAClientApi = LoadLibrary(DLL_LA_CLIENT_API);

		/*
		 * If the LA client API DLL was successfully loaded, set up the function pointers
         * to the API fuctions. The object creation is aborted if any of the expected
         * function pointers cannot be obtained, since this represents a broken DLL
         * situation, which should be flagged.
		 */
		if (m_hLAClientApi != NULL)
		{
			LACAPI_SignChallenge = (_LAClientApi_SignChallenge*) GetProcAddress(
                m_hLAClientApi, LACAPI_SIGNCHALLENGE);

			if (LACAPI_SignChallenge == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_LACAPI_SIGN_CHALLENGE));

			LACAPI_LastErrorString = (_LAClientApi_LastErrorString*) GetProcAddress(
                m_hLAClientApi, LACAPI_LASTERRORSTRING);

			if (LACAPI_LastErrorString == NULL)
				throw UTicketException(
					E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_LACAPI_LAST_ERROR_STRING));
        }
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_FINAL_CONSTRUCT);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}
	return hr;
}

/*
 * CTicket::FinalRelease()
 */
void CTicket::FinalRelease() {}

/*
 * CTicket::DestroyTicket()
 *
 * Calls the _TcktApi_destroyTicket() API.
 *
 * Parameters
 * ----------
 *
 * None
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAILED_TO_LOAD_TICKET_API
 * E_TICKET_API_ERROR
 * E_FAILED_TO_INITIALISE_TICKET_API
 */
STDMETHODIMP CTicket::DestroyTicket(void)
{
	HRESULT hr = S_OK;

	try
	{
		int res = 0;

		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

		/*
		 * Call the destroyTicket API. This destroys any ticket currently held in the ticket
		 * store. If the call succeeds, TCK_API_SUCCESS is returned. Otherwise an error code
		 * is returned. Possible error codes include TCK_API_ERR_NOTINSTALLED.
		 */
		res = IAAPI_DestroyTicket(m_lTicketApiInstance);

		if (res != TCK_API_SUCCESS)
			throw UTicketException(
				E_TICKET_API_ERROR, CComBSTR(ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED));
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_DESTROY_TICKET);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}
	return hr;
}

/*
 * CTicket::GetGAVersion()
 *
 * Calls the _TcktApi_GetGAVersion() API and passes the returned version number back to the
 * caller.
 *
 * Parameters
 * ----------
 *
 * [in] LONG componentType - specifies the GATicket system component for which the version
 * number is required. Possible component types include:
 *
 * GATICKET_VERSION_API    - retrieves the version of the GATicket API
 * GATICKET_VERSION_ENGINE - retrieves the version of the GATicket engine
 *
 * [out] LONG* version - receives the version number of the specified component
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAILED_TO_LOAD_TICKET_API
 * E_TICKET_API_ERROR
 * E_FAILED_TO_INITIALISE_TICKET_API
 */
STDMETHODIMP CTicket::GetGAVersion(LONG componentType, LONG* version)
{
	HRESULT hr = S_OK;
	long vers = 0;

	try
	{
		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

		/*
		 * Call the GetGAVersion API. If the API call is successful, then the version number
		 * of the selected component is returned. Otherwise, the API returns 0.
		 */
		vers = IAAPI_GetGaVersion(m_lTicketApiInstance, componentType);

		if (vers == 0)
			throw UTicketException(
				E_TICKET_API_ERROR, CComBSTR(ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED));
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_GET_GA_VERSION);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}

	*version = vers;
	return hr;
}

/*
 * CTicket::GetErrorDescription()
 *
 * Calls the _TcktApi_getErrorDescription() API.
 *
 * Parameters
 * ----------
 *
 * LONG error - the ticket API error code for which the description is required
 * BSTR* description - receives the description corresponding to the specified error code
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAILED_TO_LOAD_TICKET_API
 * E_TICKET_API_ERROR
 * E_FAILED_TO_INITIALISE_TICKET_API
 */
STDMETHODIMP CTicket::GetErrorDescription(LONG error, BSTR* description)
{
	HRESULT hr = S_OK;
	CComBSTR errorDescription(_T(""));

	try
	{
		int res = 0;
		unsigned int usedLen = 0;
		char szErrorDescriptionBuffer[SIZ_ERROR_DESCRIPTION_BUFFER];

		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

		res = IAAPI_GetErrorDescription(
            m_lTicketApiInstance,
            error,
            szErrorDescriptionBuffer,
            SIZ_ERROR_DESCRIPTION_BUFFER,
            &usedLen);

		if (res != TCK_API_SUCCESS)
			throw UTicketException(
				E_TICKET_API_ERROR, CComBSTR(ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED));

		if (usedLen == SIZ_ERROR_DESCRIPTION_BUFFER)
			throw UTicketException(E_FAIL, CComBSTR(ERRMSG_ERROR_DESCRIPTION_TRUNCATED));

		USES_CONVERSION;
		errorDescription = A2W(szErrorDescriptionBuffer);
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_GET_ERROR_DESCRIPTION);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}

	(void) errorDescription.CopyTo(description);
	return hr;
}

/*
 * CTicket::GetLastError()
 *
 * Calls the _TcktApi_getLastError() API.
 *
 * Parameters
 * ----------
 *
 * LONG* errorCode - the error code corresponding to the last ticket API error
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAILED_TO_LOAD_TICKET_API
 * E_FAILED_TO_INITIALISE_TICKET_API
 */
STDMETHODIMP CTicket::GetLastError(LONG* errorCode)
{
	//Retrieves the error code for the last error which occured.

	HRESULT hr = S_OK;
	long error = 0;

	try
	{
		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

		/*
		 * Call the getLastError API. The last error code generated by the ticket API is
		 * returned.
		 */
		error = IAAPI_GetLastError(m_lTicketApiInstance);
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_GET_LAST_ERROR);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}

	*errorCode = error;
	return hr;
}

/*
 * CTicket::GetNewTicket()
 *
 * Calls the _TcktApi_getNewTicket() API and passes the returned ticket back to the
 * caller.
 *
 * Parameters
 * ----------
 *
 * [out] BSTR* tokenId - receives the new ticket
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAIL
 * E_FAILED_TO_LOAD_TICKET_API
 * E_TICKET_API_ERROR
 * E_FAILED_TO_INITIALISE_TICKET_API
 */
STDMETHODIMP CTicket::GetNewTicket(BSTR* tokenId)
{
	HRESULT hr = S_OK;
	CComBSTR tokenIdentifier(_T(""));
	
	try
	{
		int res = 0;
		unsigned int usedLen = 0;
		char szTicketBuffer[SIZ_TICKET_BUFFER];

		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

		/*
		 * Call the getNewTicket API. This API requests a new ticket from the ticket
		 * engine. If the API call succeeds, the new ticket is written to the supplied
		 * buffer, usedLen is updated with the number of bytes written and TCK_API_SUCCESS
		 * is returned. Otherwise, an error code is returned and the output parameters are
		 * not altered.
		 */
		res = IAAPI_GetNewTicket(
            m_lTicketApiInstance, szTicketBuffer, SIZ_TICKET_BUFFER, &usedLen);

		if (res != TCK_API_SUCCESS)
			throw UTicketException(
				E_TICKET_API_ERROR, CComBSTR(ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED));

		if (usedLen == SIZ_TICKET_BUFFER)
			throw UTicketException(E_FAIL, CComBSTR(ERRMSG_TOKEN_IDENTIFIER_TRUNCATED));

		USES_CONVERSION;
		tokenIdentifier = A2W(szTicketBuffer);
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_GET_NEW_TICKET);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}

	(void) tokenIdentifier.CopyTo(tokenId);
	return hr;
}

/*
 * CTicket::GetTicket()
 *
 * Calls the _TcktApi_getTicket() API and passes the returned ticket back to the caller.
 *
 * Parameters
 * ----------
 *
 * [out] BSTR* tokenId - receives the current ticket in the ticket store 
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAIL
 * E_FAILED_TO_LOAD_TICKET_API
 * E_TICKET_API_ERROR
 * E_FAILED_TO_INITIALISE_TICKET_API
 */
STDMETHODIMP CTicket::GetTicket(BSTR* tokenId)
{
	HRESULT hr = S_OK;
	CComBSTR tokenIdentifier(_T(""));
	
	try
	{
		int res = 0;
		unsigned int usedLen = 0;
		char szTicketBuffer[SIZ_TICKET_BUFFER];

		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

		/*
		 * Call the getTicket API to read the current ticket from the ticket engine. If there
		 * is no current ticket, the engine will organise one. If the API call succeeds, the
		 * ticket is written to the supplied buffer, usedLen is updated with the number of
		 * bytes written and TCK_API_SUCCESS is returned. Otherwise, an error code is returned
		 * and the output parameters are not altered.
		 */
		res = IAAPI_GetTicket(
            m_lTicketApiInstance, szTicketBuffer, SIZ_TICKET_BUFFER, &usedLen);

		if (res != TCK_API_SUCCESS)
		{
			if (res == TCK_API_ERR_BUFFER_TOO_SMALL)
			{
				/*
				 * The buffer supplied to receive the ticket was not large enough. Repeat
				 * the API call, this time supplying a larger buffer. We do not check for
				 * the TCK_API_ERR_BUFFER_TOO_SMALL error again on this second attempt.
				 */
				char szExtendedTicketBuffer[SIZ_EXTENDED_TICKET_BUFFER];

				res = IAAPI_GetTicket(
                    m_lTicketApiInstance,
                    szExtendedTicketBuffer,
                    SIZ_EXTENDED_TICKET_BUFFER,
                    &usedLen);

				if (res != TCK_API_SUCCESS)
					throw UTicketException(
						E_TICKET_API_ERROR,
						CComBSTR(ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED));

				if (usedLen == SIZ_EXTENDED_TICKET_BUFFER)
					throw UTicketException(E_FAIL, CComBSTR(ERRMSG_TOKEN_IDENTIFIER_TRUNCATED));

				USES_CONVERSION;
				tokenIdentifier = A2W(szExtendedTicketBuffer);
			}
			else
			{
				// The getTicket API call failed with some other error code.
				if (res != TCK_API_SUCCESS)
					throw UTicketException(
						E_TICKET_API_ERROR,
						CComBSTR(ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED));
			}
		}
		else
		{
			if (usedLen == SIZ_TICKET_BUFFER)
				throw UTicketException(E_FAIL, CComBSTR(ERRMSG_TOKEN_IDENTIFIER_TRUNCATED));

			USES_CONVERSION;
			tokenIdentifier = A2W(szTicketBuffer);

			hr = S_OK;
		}
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_GET_TICKET);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}

	(void) tokenIdentifier.CopyTo(tokenId);
	return hr;
}

/*
 * CTicket::GetTicketNoAuth()
 *
 * Calls the _TcktApi_getTicketNoAuth() API and passes the returned ticket back to the caller.
 *
 * Parameters
 * ----------
 *
 * [out] BSTR* tokenId - receives the current ticket in the ticket store 
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAIL
 * E_FAILED_TO_LOAD_TICKET_API
 * E_TICKET_API_ERROR
 * E_FAILED_TO_INITIALISE_TICKET_API
 */
STDMETHODIMP CTicket::GetTicketNoAuth(BSTR* tokenId)
{
	HRESULT hr = S_OK;
	CComBSTR tokenIdentifier(_T(""));
	
	try
	{
		int res = 0;
		unsigned int usedLen = 0;
		char szTicketBuffer[SIZ_TICKET_BUFFER];

		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

		/*
		 * Call the getTicketNoAuth API to read the current ticket from the ticket engine. If
		 * there is no current ticket, error code TCK_API_ERR_TICKET_NOT_AVAILABLE will be
		 * returned. If the API call succeeds, the ticket is written to the supplied buffer,
		 * usedLen is updated with the number of bytes written and TCK_API_SUCCESS is returned.
		 * Otherwise, an error code is returned and the output parameters are not altered.
		 */
		res = IAAPI_GetTicketNoAuth(
            m_lTicketApiInstance, szTicketBuffer, SIZ_TICKET_BUFFER, &usedLen);

		if (res != TCK_API_SUCCESS)
		{
			if (res == TCK_API_ERR_BUFFER_TOO_SMALL)
			{
				/*
				 * The buffer supplied to receive the ticket was not large enough. Repeat
				 * the API call, this time supplying a larger buffer. We do not check for
				 * the TCK_API_ERR_BUFFER_TOO_SMALL error again on this second attempt.
				 */
				char szExtendedTicketBuffer[SIZ_EXTENDED_TICKET_BUFFER];

				res = IAAPI_GetTicketNoAuth(
                    m_lTicketApiInstance,
                    szExtendedTicketBuffer,
                    SIZ_EXTENDED_TICKET_BUFFER,
                    &usedLen);

				if (res != TCK_API_SUCCESS)
					throw UTicketException(
						E_TICKET_API_ERROR,
						CComBSTR(ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED));

				if (usedLen == SIZ_EXTENDED_TICKET_BUFFER)
					throw UTicketException(E_FAIL, CComBSTR(ERRMSG_TOKEN_IDENTIFIER_TRUNCATED));

				USES_CONVERSION;
				tokenIdentifier = A2W(szExtendedTicketBuffer);
			}
			else
			{
				// The getTicketNoAuth API call failed with some other error code.
				if (res != TCK_API_SUCCESS)
					throw UTicketException(
						E_TICKET_API_ERROR,
						CComBSTR(ERRMSG_IDENTITY_AGENT_TICKET_API_CALL_FAILED));
			}
		}
		else
		{
			if (usedLen == SIZ_TICKET_BUFFER)
				throw UTicketException(E_FAIL, CComBSTR(ERRMSG_TOKEN_IDENTIFIER_TRUNCATED));

			USES_CONVERSION;
			tokenIdentifier = A2W(szTicketBuffer);

			hr = S_OK;
		}
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_GET_TICKET);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}

	(void) tokenIdentifier.CopyTo(tokenId);
	return hr;
}

/*
 * CTicket::Finalize()
 *
 * Calls the _TcktApi_Finalize() API.
 *
 * Parameters
 * ----------
 *
 * None
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAIL
 * E_FAILED_TO_LOAD_TICKET_API
 * E_FAILED_TO_INITIALISE_TICKET_API
 */
STDMETHODIMP CTicket::Finalize(void)
{
	HRESULT hr = S_OK;

	try
	{
		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

		/*
		 * Call the Finalize API to finalise the ticket API. After this call, any further
		 * calls to the ticket API methods of this object will fail with the result code
		 * E_FAILED_TO_INITIALISE_TICKET_API.
		 */
		IAAPI_Finalize(m_lTicketApiInstance);
		m_lTicketApiInstance = 0;
	}
	catch (UTicketException e)
	{
		CComBSTR bstrExMsg(ITICKET_FINALIZE);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}
	return hr;
}

/*
 * CTicket::SignChallenge()
 *
 * Calls the LA client SignChallenge() API.
 *
 * Parameters
 * ----------
 *
 * [in] BSTR bstrChallenge - the challenge to be signed
 * [out] BSTR* pbstrSignedChallenge - receives the signed challenge 
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAIL
 * E_FAILED_TO_LOAD_TICKET_API
 * E_FAILED_TO_INITIALISE_TICKET_API
 * E_FAILED_TO_LOAD_LA_CLIENT_API
 */
STDMETHODIMP CTicket::SignChallenge(BSTR bstrChallenge, BSTR* pbstrSignedChallenge)
{
	HRESULT hr = S_OK;
	
	try
	{
		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the Local Authenticator client API has been loaded.
		if (m_hLAClientApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_LA_CLIENT_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_LOCAL_AUTHENTICATOR_CLIENT_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

        // Check that the supplied challenge string is not null.
        if (bstrChallenge == NULL)
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_NO_CHALLENGE_STRING_SUPPLIED));

        // Create a CComBSTR object to contain the challenge string to be signed.
       	CComBSTR ccbstrChallenge(bstrChallenge);

        // Get the length of the supplied challenge string.
        unsigned int uiChallengeLen = ccbstrChallenge.Length();

        // Check that the challenge string is not empty.
        if (uiChallengeLen == 0)
            throw UTicketException(
                E_FAIL, CComBSTR(ERRMSG_EMPTY_CHALLENGE_STRING_SUPPLIED));

        // Check that the challenge string is not longer than the maximum expected length.
        if (uiChallengeLen > (MAX_CHALLENGE_STRING_BYTES - 1))
            throw UTicketException(E_FAIL, CComBSTR(ERRMSG_CHALLENGE_STRING_TOO_LONG));

        // Convert the challenge string from a wide string to a multibyte string.
        COLE2CTEX<MAX_CHALLENGE_STRING_BYTES> pszChallenge(ccbstrChallenge.m_str);

        // Create a buffer to receive the signed challenge.
        TCHAR szSignedChallenge[SIZ_SIGNED_CHALLENGE_BUFFER];

        // Call the LA client SignChallenge API to sign the challenge.
        int iResultCount;
        int iResultCode = LACAPI_SignChallenge(pszChallenge, szSignedChallenge, &iResultCount);

        // Check the result code from the signing operation.
        if (iResultCode != LA_ERR_SUCCESS)
            throw UTicketException(
                E_FAIL, CComBSTR(ERRMSG_LOCAL_AUTHENTICATOR_CLIENT_API_CALL_FAILED));
        // TODO: extend error handling to report result code & result count.

        // Create a CComBSTR object to contain the signed challenge.
        CComBSTR ccbstrSignedChallenge(szSignedChallenge);

        // Copy the signed challenge to the output parameter.
        HRESULT hrResultCode = ccbstrSignedChallenge.CopyTo(pbstrSignedChallenge);

        // Check the result of the copy operation.
        if (hrResultCode != S_OK)
            throw UTicketException(E_FAIL, CComBSTR(ERRMSG_FAILED_TO_COPY_SIGNED_CHALLENGE));
	}
	catch (UTicketException e)
	{
        // Pass an empty string back to the caller.
        (void) CComBSTR(_T("")).CopyTo(pbstrSignedChallenge);

        // Handle the exception.
		CComBSTR bstrExMsg(ITICKET_SIGN_CHALLENGE);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}
	return hr;
}

/*
 * CTicket::GetLastLAError()
 *
 * Calls the Local Authenticator client LastErrorString API.
 *
 * Parameters
 * ----------
 *
 * [out] BSTR* pbstrLastLAError - receives the last error message generated by the Local
 * Authenticator client API.
 *
 * Returns
 * -------
 *
 * S_OK
 * E_FAIL
 * E_FAILED_TO_LOAD_TICKET_API
 * E_FAILED_TO_INITIALISE_TICKET_API
 * E_FAILED_TO_LOAD_LA_CLIENT_API
 */

STDMETHODIMP CTicket::GetLastLAError(BSTR* pbstrLastLAError)
{
	HRESULT hr = S_OK;
	
	try
	{
		// Check that the Identity Agent ticket API has been loaded.
		if (m_hTicketApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_IDENTITY_AGENT_TICKET_API));

		// Check that the Identity Agent ticket API has been initialised.
		if (m_lTicketApiInstance == 0)
			throw UTicketException(
				E_FAILED_TO_INITIALISE_TICKET_API,
				CComBSTR(ERRMSG_FAILED_TO_INITIALISE_IDENTITY_AGENT_TICKET_API));

		// Check that the Local Authenticator client API has been loaded.
		if (m_hLAClientApi == NULL)
			throw UTicketException(
				E_FAILED_TO_LOAD_LA_CLIENT_API,
				CComBSTR(ERRMSG_FAILED_TO_LOAD_LOCAL_AUTHENTICATOR_CLIENT_API));

		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));

        // Call the LA client LastErrorString API to obtain the last error string.
        char* pszLastLAError = LACAPI_LastErrorString();

        // Create a CComBSTR object to contain the last error string.
        CComBSTR ccbstrLastLAError(TSTR_EMPTY);

        // If the API returned a non-null string, assign it to our CComBSTR object.
        if (pszLastLAError != NULL) ccbstrLastLAError = pszLastLAError;

        // Copy the error string to the output parameter.
        HRESULT hrResultCode = ccbstrLastLAError.CopyTo(pbstrLastLAError);

        // Check the result of the copy operation.
        if (hrResultCode != S_OK)
            throw UTicketException(E_FAIL, CComBSTR(ERRMSG_FAILED_TO_COPY_LAST_ERROR_STRING));
	}
	catch (UTicketException e)
	{
        // Pass an empty string back to the caller.
        (void) CComBSTR(TSTR_EMPTY).CopyTo(pbstrLastLAError);

        // Handle the exception.
		CComBSTR bstrExMsg(ITICKET_GET_LAST_LA_ERROR);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}
	return hr;
}

/*
 * CTicket::GetProductVersion()
 *
 * Returns the product version string for the IATicket object.
 *
 * Parameters
 * ----------
 *
 * BSTR* pbstrProductVersion - receives the product version string
 *
 * Returns
 * -------
 *
 * S_OK
 * E_TICKET_API_ERROR
 */
STDMETHODIMP CTicket::GetProductVersion(BSTR* pbstrProductVersion)
{
	HRESULT hr = S_OK;
	HMODULE hmModule = NULL;
	
	try
	{
		// Check that the object is being invoked from an authorised URL domain.
		if (!IsAuthorisedHostDomain())
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_HOST_URL_DOMAIN_IS_UNAUTHORISED));
		
		/* Get a handle to our own executable module - this requires a reflexive call to
		 * LoadLibrary. */
		hmModule = LoadLibrary(DLL_IA_TICKET);

		if (hmModule == NULL)
			throw UTicketException(E_FAIL, CComBSTR(ERRMSG_FAILED_TO_OBTAIN_MODULE_HANDLE));

		// Get a handle to the version info resource.
		HRSRC hrscVersionInfo =
			FindResource(hmModule, MAKEINTRESOURCE(VS_VERSION_INFO), RT_VERSION);

		if (hrscVersionInfo == NULL)
            throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_FAILED_TO_FIND_VERSION_INFO_RESOURCE));

		// Load the version info resource.
		HGLOBAL hglobVersionInfo = LoadResource(hmModule, hrscVersionInfo);

		if (hglobVersionInfo == NULL)  
            throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_FAILED_TO_LOAD_VERSION_INFO_RESOURCE));

		// Lock the version info resource.
		LPVOID pvVersionInfo  = LockResource(hglobVersionInfo);

		if (pvVersionInfo == NULL)
            throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_FAILED_TO_LOCK_VERSION_INFO_RESOURCE));

		// Query the version info resource for the translation information.
		LPVOID pvTranslationInfo;
		UINT uiTranslationInfoLen;

		BOOL bRV = VerQueryValue(
			pvVersionInfo,
			(LPTSTR) RSRCPATH_VARFILEINFO_TRANSLATION,
			&pvTranslationInfo,
			&uiTranslationInfoLen);

		if (!bRV)
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_FAILED_TO_GET_TRANSLATION_INFORMATION));

		// Copy the bytes of the translation information into a DWORD variable.
		DWORD dwTranslationInfo;
		(void) memcpy(&dwTranslationInfo, pvTranslationInfo, SIZ_TRANSLATION_INFO);

		// Format the product version resource path.
		static TCHAR szProdVerRsrcPathBuf[SIZ_PRODUCT_VERSION_RESOURCE_PATH_BUFFER];
		(void) sprintf_s(
			szProdVerRsrcPathBuf,
			RSRCPATH_STRINGFILEINFO_PRODUCTVERSION,
			(dwTranslationInfo & MASK_DWORD_WORD_1),
			(dwTranslationInfo >> SHIFT_DWORD_WORD_0));

		// Query the version info resource for the product version information.
		LPVOID pvProductVersion;
		UINT uiProductVersionLen;

		bRV = VerQueryValue(
			pvVersionInfo,
			szProdVerRsrcPathBuf,
			&pvProductVersion,
			&uiProductVersionLen);

		if (!bRV || (uiProductVersionLen == 0))
			throw UTicketException(
				E_FAIL, CComBSTR(ERRMSG_FAILED_TO_GET_PRODUCT_VERSION));

		TCHAR* pszProductVersion = (TCHAR*) pvProductVersion;

        // Create a CComBSTR object to contain the product version string.
        CComBSTR ccbstrProductVersion(TSTR_EMPTY);

        // Initialise the CComBSTR object from the product version buffer.
        ccbstrProductVersion = pszProductVersion;

        // Copy the product version to the output parameter.
        HRESULT hrResultCode = ccbstrProductVersion.CopyTo(pbstrProductVersion);

        // Check the result of the copy operation.
        if (hrResultCode != S_OK)
            throw UTicketException(E_FAIL, CComBSTR(ERRMSG_FAILED_TO_COPY_PRODUCT_VERSION));
	}
	catch (UTicketException e)
	{
        // Pass an empty string back to the caller.
        (void) CComBSTR(TSTR_EMPTY).CopyTo(pbstrProductVersion);

        // Handle the exception.
		CComBSTR bstrExMsg(ITICKET_GET_PRODUCT_VERSION);
		bstrExMsg += e.GetMessage();
		hr = e.m_hr;
		AtlReportError(__uuidof(CTicket), bstrExMsg, __uuidof(ITicket), hr);
	}
	return hr;
}

/*
 * CTicket::SetSite()
 * IObjectWithSite::SetSite()
 *
 * Receives an IUnknown pointer to the ActiveX object's container site.
 *
 * Parameters
 * ----------
 *
 * [in] pUnkSite - contains the site pointer of the object.
 *
 * Returns
 * -------
 *
 * S_OK
 */
STDMETHODIMP CTicket::SetSite(IUnknown* pUnkSite)
{
	// Save the passed-in site pointer in member data.
	m_pUnkSite = pUnkSite;
	return S_OK;
};

/*
 * CTicket::GetSite()
 * IObjectWithSite::GetSite()
 *
 * Retrieves an IUnknown pointer to the ActiveX object's container site.
 *
 * Parameters
 * ----------
 *
 * [in] riid - the IID of the interface pointer that should be returned in ppvSite.
 * [out] ppvSite - the required interface pointer.
 *
 * Returns
 * -------
 *
 * Forwarded return from IUnknown::QueryInterface().
 */
STDMETHODIMP CTicket::GetSite(REFIID riid, LPVOID* ppvSite)
{
	return m_pUnkSite->QueryInterface(riid, ppvSite);
}

/*
 * CTicket::IsAuthorisedHostDomain()
 *
 * Determines whether the ActiveX control is being hosted from a URL domain that is defined
 * as being an authorised domain.
 *
 * Parameters
 * ----------
 *
 * None.
 *
 * Returns
 * -------
 *
 * True if the ActiveX control is being hosted from an authorised host domain; false otherwise.
 */
bool CTicket::IsAuthorisedHostDomain()
{
	char szUrl[INTERNET_MAX_URL_LENGTH];

	if (!GetHostUrl(szUrl)) return false;

	return IsAuthorisedHostDomain(szUrl);
}

/*
 * CTicket::GetHostUrl()
 *
 * Returns the URL from which the ActiveX control is being hosted.
 *
 * Parameters
 * ----------
 *
 * [out] pszUrl - receives the URL that is hosting the ActiveX control.
 *
 * Returns
 * -------
 *
 * True on success; false otherwise.
 */
bool CTicket::GetHostUrl(char* pszUrl)
{
	CComPtr<IServiceProvider> ccompServiceProvider;
	CComPtr<IWebBrowser2> ccompWebBrowser2;

	if (FAILED(GetSite(IID_IServiceProvider, (void**) &ccompServiceProvider))) return false;

	if
		(FAILED(ccompServiceProvider->QueryService(
			SID_SWebBrowserApp, IID_IWebBrowser2, (void**) &ccompWebBrowser2)))
				return false;

	CComBSTR bstrUrl;
	if (FAILED(ccompWebBrowser2->get_LocationURL(&bstrUrl))) return false;

	// Convert the URL from UNICODE to ASCII.
	CW2A pszUrlA(bstrUrl.m_str);

	// Copy the URL into the output parameter.
	if (strcpy_s(pszUrl, INTERNET_MAX_URL_LENGTH, pszUrlA) != 0) return false;

	return true;
}

/*
 * CTicket::IsAuthorisedHostDomain()
 *
 * Determines whether the supplied URL is authorised for hosting the ActiveX control.
 *
 * Parameters
 * ----------
 *
 * [out] pszUrl - the URL to be checked.
 *
 * Returns
 * -------
 *
 * True if the domain of the supplied URL is authorised to host the ActiveX control; false
 * otherwise.
 */
bool CTicket::IsAuthorisedHostDomain(char* pszUrl)
{
	// Only the HTTP and HTTPS schemes are authorised.
	if
		((GetUrlScheme(pszUrl) != INTERNET_SCHEME_HTTP)
		&&
		(GetUrlScheme(pszUrl) != INTERNET_SCHEME_HTTPS))
			return false;
	// TODO: disallow HTTP in production.

	char szDomain[256];

	if (!GetUrlDomain(pszUrl, szDomain, sizeof(szDomain))) return false;

	for (int i = 0; i < ARRAYSIZE(m_ppszAuthorisedUrlDomains); i++)
	{
		if (IsDomainMatch(const_cast<char*> (m_ppszAuthorisedUrlDomains[i]), szDomain))
			return true;
	}
	return false;
}

/*
 * CTicket::GetUrlScheme()
 *
 * Extracts the URL scheme from the supplied URL and returns it to the caller.
 *
 * Parameters
 * ----------
 *
 * [in] pszUrl - the URL from which the URL scheme is to be extracted.
 *
 * Returns
 * -------
 *
 * An INTERNET_SCHEME item indicating the URL scheme of the supplied URL.
 */
INTERNET_SCHEME CTicket::GetUrlScheme(char* pszUrl)
{
	char szUrlSchemeBuffer[SIZ_URL_SCHEME_BUFFER];
	URL_COMPONENTS uc;

	ZeroMemory(&uc, sizeof uc);

	uc.dwStructSize = sizeof uc;
	uc.lpszScheme = szUrlSchemeBuffer;
	uc.dwSchemeLength = SIZ_URL_SCHEME_BUFFER;

	if (InternetCrackUrl(pszUrl, lstrlen(pszUrl), ICU_DECODE, &uc))
		return uc.nScheme;
	else
		return INTERNET_SCHEME_UNKNOWN;
}

/*
 * CTicket::GetUrlDomain()
 *
 * Extracts the URL domain from the supplied URL and returns it to the caller.
 *
 * Parameters
 * ----------
 *
 * [in] pszUrl - the URL from which the URL domain is to be extracted.
 * [out] pszBuffer - pointer to a buffer into which the URL scheme will be written.
 * [in] iBufferSize - the size of the supplied buffer.
 *
 * Returns
 * -------
 *
 * True on success; false otherwise.
 */
bool CTicket::GetUrlDomain(char* pszUrl, char* pszBuffer, int iBufferSize)
{
	URL_COMPONENTS uc;
	ZeroMemory(&uc, sizeof uc);

	uc.dwStructSize = sizeof uc;
	uc.lpszHostName = pszBuffer;
	uc.dwHostNameLength = iBufferSize;

	return (InternetCrackUrl(pszUrl, lstrlen(pszUrl), ICU_DECODE, &uc) == TRUE);
}

// Return if ourDomain is within approvedDomain.
// approvedDomain must either match ourDomain
// or be a suffix preceded by a dot.
// 
/*
 * CTicket::IsDomainMatch()
 *
 * Determines whether the supplied test URL domain matches the supplied reference URL domain.
 *
 * Parameters
 * ----------
 *
 * [in] pszRefDomain - pointer to the reference URL domain.
 * [in] pszTestDomain - pointer to the test URL domain.
 *
 * Returns
 * -------
 *
 * True if the test URL domain matches the reference URL domain; false otherwise.
 */
bool CTicket::IsDomainMatch(char* pszRefDomain, char* pszTestDomain)
{
	int iRefDomainLen  = lstrlen(pszRefDomain);
	int iTestDomainLen = lstrlen(pszTestDomain);

	if (iRefDomainLen > iTestDomainLen) return false;

	if (lstrcmpi(pszTestDomain + iTestDomainLen - iRefDomainLen, pszRefDomain) != 0)
		return false;

	if (iRefDomainLen == iTestDomainLen) return true;

	if (pszTestDomain[iTestDomainLen - iRefDomainLen - 1] == '.') return true;

	return false;
}
