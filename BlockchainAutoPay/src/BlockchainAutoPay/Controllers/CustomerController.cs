using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using BlockchainAutoPay.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace BlockchainAutoPay.Controllers
{
    [Route("api/Customer")]
    [Produces("application/json")]
    [EnableCors("AllowDevelopmentEnvironment")]
    public class CustomerController : Controller
    {
        private BCAPContext _context;

        public CustomerController(BCAPContext context)
        {
            _context = context;
        }

        // GET: api/values
        [HttpGet]
        //[Route("api/Customer")]
        //[Produces("application/json")]
        //[EnableCors("AllowDevelopmentEnvironment")]
        public IActionResult Get()
        {
            var currentCustomer = from c in _context.CurrentCustomer
                                  select c;
            return Ok(currentCustomer);
        }

        //// GET: api/values
        //[HttpGet]
        //public IActionResult GetCustomer([FromQuery]string id, string customerName)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    //IQueryable<Customer> customer = from c in _context.Customer
        //    //                                    // where c.CustomerId == id
        //    //                                select new Customer
        //    //                                {
        //    //                                    CustomerId = c.CustomerId,
        //    //                                    CustomerName = c.CustomerName,
        //    //                                    CreatedDate = c.CreatedDate,
        //    //                                    Location = c.Location,
        //    //                                    Email = c.Email,
        //    //                                    FavoriteTracks = from t in _context.Track
        //    //                                                     join al in _context.Album on t.AlbumId equals al.AlbumId
        //    //                                                     join ar in _context.Artist on al.ArtistId equals ar.ArtistId
        //    //                                                     select new TrackInfo
        //    //                                                     {
        //    //                                                         AlbumTitle = al.AlbumTitle,
        //    //                                                         YearReleased = al.YearReleased,
        //    //                                                         Author = t.Author,
        //    //                                                         Genre = t.Genre,
        //    //                                                         Title = t.Title
        //    //                                                     }
        //    //                                };

        //    IQueryable<CurrentCustomer> currentUser = from c in _context.CurrentCustomer
        //                                              select new CurrentCustomer
        //                                              {
        //                                                  CustomerId = c.CustomerId,
        //                                                  Data = c.Data
        //                                              };
        //    if (id != null)
        //    {
        //        currentUser = currentUser.Where(cus => cus.CustomerId == id);
        //    }

        //    if (currentUser == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(currentUser);
        //}

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody]Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = from g in _context.Customer
                               where g.CustomerName == customer.CustomerName
                               select g;

            if (existingUser.Count<Customer>() > 0)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            _context.Customer.Add(customer);
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                if (CustomerExists(customer.CustomerId))
                {
                    return new StatusCodeResult(StatusCodes.Status409Conflict);
                }
                else
                {
                    throw;
                }
            }

            // return CreatedAtRoute("GetCustomer", new { id = geek.GeekId }, geek);
            return Ok(existingUser);
        }


        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody]Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != customer.CustomerId)
            {
                return BadRequest();
            }

            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customer.CustomerId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return new StatusCodeResult(StatusCodes.Status204NoContent);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Customer customer = _context.Customer.Single(c => c.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }

            _context.Customer.Remove(customer);
            _context.SaveChanges();

            return Ok(customer);
        }

        private bool CustomerExists(int id)
        {
            return _context.Customer.Count(c => c.CustomerId == id) > 0;
        }
    }
}
