using CineMatch.Application.Features.Admin.Common.Dtos;
using ErrorOr;
using MediatR;

namespace CineMatch.Application.Features.Admin.Queries.GetAllWatchParties;

public record GetAllWatchPartiesQuery : IRequest<ErrorOr<List<AdminWatchPartyDto>>>;
