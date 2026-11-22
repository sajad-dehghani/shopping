using Azma.Domain.Sales;
using System.Linq.Expressions;

namespace Azma.Application.Catalog;

public static class SeatQueries
{
    /// <summary>An enrollment occupies a seat if confirmed, or unpaid but still inside its reservation window.</summary>
    public static Expression<Func<Enrollment, bool>> OccupiesSeat(DateTime utcNow)
        => e => e.Status == EnrollmentStatus.Confirmed
                || (e.Status == EnrollmentStatus.PendingPayment && e.ReservedUntil != null && e.ReservedUntil > utcNow);
}
