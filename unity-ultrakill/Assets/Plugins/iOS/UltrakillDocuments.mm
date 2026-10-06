#import <Foundation/Foundation.h>

extern "C" void UltrakillGetDocumentsPath(char* buffer, int size)
{
    if (buffer == NULL || size <= 0)
    {
        return;
    }

    NSArray* paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
    if (paths.count == 0)
    {
        buffer[0] = '\0';
        return;
    }

    NSString* docs = [paths objectAtIndex:0];
    const char* utf8 = [docs UTF8String];
    strncpy(buffer, utf8, size - 1);
    buffer[size - 1] = '\0';
}
