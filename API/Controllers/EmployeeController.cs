using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using Core.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Core.Models;
using API.DTOS;
using Core.Specification;
using API.Helper;

namespace API.Controllers
{
    public class EmployeeController : BaseController
    {
        private readonly IConfiguration _config;
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IUOW uow, IMapper mapper, IConfiguration config, IEmployeeService employeeService) : base(uow, mapper)
        {
            this._employeeService = employeeService;
            this._config = config;
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {

            if (file == null)
                return BadRequest();

            var tempPath = Path.GetTempPath() + file.FileName;
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {

                await file.CopyToAsync(fileStream);
            }

            NPOIService npoi = new NPOIService(tempPath);

            List<string> Sheets = npoi.GetSheetsName();
            DataTable dt = npoi.ReadSheets(Sheets);
            List<Employee> employees = _employeeService.EmployeeTableToEntity(dt);

            foreach (var employee in employees)
            {
                if (!await this._uow.EmployeeRepository.CheckEmployeeExistByNationalId(employee.NationalId))
                {

                    try
                    {
                        _uow.EmployeeRepository.Add(employee);
                    }
                    catch (System.Exception ex)
                    {

                        throw ex;
                    }


                }
                else
                {
                    ///TODOO UPDATE Department
                    // var empFromDb = await _uow.EmployeeRepository.GetEmployeeByNationalId(employee.NationalId);
                    // if (empFromDb != null)
                    // {
                    //     empFromDb.GetHashCode
                    //     _uow.EmployeeRepository.Update(employee);

                    // }

                }
            }
            await _uow.SaveChangesAsync();

            return Ok();
        }
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Employee>>> GetEmployees([FromQuery] EmployeeParam empParam)
        {

            empParam.IncludeDpertment = true;
            // var empParam = new EmployeeParam();
            var spec = new EmployeeWithSpecification(empParam);

            var countSpec = new EmployeeWithCountAsyncSpecification(empParam);
            IReadOnlyList<Employee> employees = await _uow.EmployeeRepository.GetAll(spec);
            int count = await _uow.EmployeeRepository.CountAsync(countSpec);

            IReadOnlyList<EmployeeDto> employeeDtos = _mapper.Map<IReadOnlyList<EmployeeDto>>(employees);


            return Ok(new Pagination<EmployeeDto>(empParam.PageIndex, empParam.PageSize, count, employeeDtos));
        }
        [HttpPut]

        public async Task<ActionResult<IReadOnlyList<Employee>>> PutEmployee([FromBody] EmployeeDto employee)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            Employee empToDb = _mapper.Map<Employee>(employee);
            _uow.EmployeeRepository.Update(empToDb);
            await _uow.SaveChangesAsync();
            return Ok();

        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEmployee(Guid id)
        {

            var empToDb = await _uow.EmployeeRepository.GetById(id);
            if (empToDb == null)
                return BadRequest();


            _uow.EmployeeRepository.Remove(empToDb);
            await _uow.SaveChangesAsync();
            return Ok();
        }
    }
}
