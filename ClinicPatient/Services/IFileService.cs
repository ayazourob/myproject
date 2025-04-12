﻿using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ClinicPatient.Services
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        void DeleteFile(string filePath);
    }
}
