function GetCookies() {
    const cookies = document.cookie;
    const bufferSize = lengthBytesUTF8(cookies) + 1;
    const buffer = _malloc(bufferSize);
    stringToUTF8(cookies, buffer, bufferSize);
    return buffer;
}

function SetCookies(cookies) {
    document.cookie = UTF8ToString(cookies);
}

mergeInto(LibraryManager.library, {
    GetCookies,
    SetCookies,
})