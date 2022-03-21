// chlorine.dll

#include <windows.h>
#include <iostream>
#define GameAssemblyModule L"GameAssembly.dll"

DWORD WINAPI itsaFeature(HMODULE hModule)
{ 
    // handle to our game assembly module
    uintptr_t moduleBase = (uintptr_t)GetModuleHandle(GameAssemblyModule);
    if (moduleBase)
    {
        // patch memory
        DWORD oldprotect;
        VirtualProtect((BYTE*)moduleBase + 0x1859FB4, 6, PAGE_EXECUTE_READWRITE, &oldprotect);

        // payload code to patch in
        // "\x90\x90\x90\x90\x90\x90";
        memset((BYTE*)moduleBase + 0x1859FB4, 0x90, 6);

        // ok bye
        VirtualProtect((BYTE*)moduleBase + 0x1859FB4, 6, oldprotect, &oldprotect);
        Sleep(5);
    }
    FreeLibraryAndExitThread(hModule, 0);
    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH:
        CloseHandle(CreateThread(nullptr, 0, (LPTHREAD_START_ROUTINE)itsaFeature, hModule, 0, nullptr));
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}