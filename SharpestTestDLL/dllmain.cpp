// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <stdio.h>
#include <format>

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            MessageBox(NULL, L"This DLL has been successfully injected!", L"Injection Success!", MB_OK | MB_ICONINFORMATION);
            break;
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
            break;
        case DLL_PROCESS_DETACH:
            MessageBox(NULL, L"This DLL has been successfully unloaded!", L"Unload Success!", MB_OK | MB_ICONINFORMATION);
            break;
    }
    return TRUE;
}

static int CalledNo = 0;
static int CalledNo1 = 0;
static int CalledNo2 = 0;
static int CalledNo6 = 0;

extern "C" {
    __declspec(dllexport) void HelloWorld()
    {
        MessageBox(NULL, std::format(L"Congratulations, you've successfully called an export function with no parameters! You've called this function {} time(s).", ++CalledNo).c_str(), L"Hello, World!", MB_OK | MB_ICONINFORMATION);
    }
    __declspec(dllexport) void HelloWorld1(LPCWSTR string)
    {
        MessageBox(NULL, std::format(L"Congratulations, you've successfully called an export function with a single parameter! You've called this function {} time(s). The parameter was: \"{}\"", ++CalledNo1, string).c_str(), L"Hello, World!", MB_OK | MB_ICONINFORMATION);
    }
    __declspec(dllexport) void HelloWorld2(LPCWSTR string, LPCWSTR string2)
    {
        MessageBox(NULL, std::format(L"Congrats, you've successfully called an export function with two parameters! At this point you're pretty much a god! You've called this function {} time(s). The parameters were: \"{}\" and \"{}\"", ++CalledNo2, string, string2).c_str(), L"Hello, World!", MB_OK | MB_ICONINFORMATION);
    }
    __declspec(dllexport) void HelloWorld7(LPCWSTR string, LPCWSTR string2, LPCWSTR string3, LPCWSTR string4, LPCWSTR string5, LPCWSTR string6, LPCWSTR string7)
    {
        MessageBox(NULL, std::format(L"Yeah, this is just witchcraft at this point. You've called this function {} time(s). The parameters were: \"{}\", \"{}\", \"{}\", \"{}\", \"{}\", \"{}\", \"{}\"", ++CalledNo6, string, string2, string3, string4, string5, string6, string7).c_str(), L"Hello, World!", MB_OK | MB_ICONINFORMATION);
    }
    __declspec(dllexport) void HelloWorldL(LPCWSTR string, DWORD *dword, FLOAT *float1)
    {
        MessageBox(NULL, std::format(L"This function takes in a string ({}), dword pointer ({}) and a float pointer ({})", string, *dword, *float1).c_str(), L"Hello, World!", MB_OK | MB_ICONINFORMATION);
    }
}

__declspec(dllexport) void HelloWorldCpp(LPCWSTR string, DWORD* dword, FLOAT* float1)
{
    MessageBox(NULL, std::format(L"This function takes in a string ({}), dword pointer ({}) and a float pointer ({}). C++ version so I can mess with decorators.", string, *dword, *float1).c_str(), L"Hello, World!", MB_OK | MB_ICONINFORMATION);
}

__declspec(dllexport) DWORD HelloWorldOutTest(LPCWSTR string)
{
    MessageBox(NULL, std::format(L"This function takes in a string ({}), and returns the answer to life, the universe, and everything.", string).c_str(), L"Hello, World!", MB_OK | MB_ICONINFORMATION);
    return 42;
}
