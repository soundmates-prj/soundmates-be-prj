using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
