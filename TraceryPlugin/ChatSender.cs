﻿using System;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

// From: https://git.anna.lgbt/anna/XivCommon/src/branch/main/XivCommon/Functions/Chat.cs viat ChatTwo
public unsafe class ChatSender
{
    public static void SendMessageUnsafe(byte[] message)
    {
        var mes = Utf8String.FromSequence(message);
        UIModule.Instance()->ProcessChatBoxEntry(mes);
        mes->Dtor(true);
    }

    public static void SendMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        if (bytes.Length == 0)
            throw new ArgumentException("message is empty", nameof(message));

        if (bytes.Length > 500)
            throw new ArgumentException("message is longer than 500 bytes", nameof(message));

        if (message.Length != SanitiseText(message).Length)
            throw new ArgumentException("message contained invalid characters", nameof(message));

        SendMessageUnsafe(bytes);
    }

    private static string SanitiseText(string text)
    {
        var uText = Utf8String.FromString(text);

        uText->SanitizeString(
            AllowedEntities.UppercaseLetters 
            | AllowedEntities.LowercaseLetters
            | AllowedEntities.Numbers
            | AllowedEntities.SpecialCharacters
            //Payloads? Something to think about later. For now, no.
        );
        var sanitised = uText->ToString();
        uText->Dtor(true);

        return sanitised;
    }
}